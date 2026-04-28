using System.Collections;
using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG
using Scripts.Floor3.Core;
using Scripts.Floor3.AI;

namespace Scripts.Floor3.Gameplay
{
    // ĐỔI SANG NetworkBehaviour
    public class RobotController : NetworkBehaviour
    {
        [Header("Waypoints")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private bool[] _isCheckpoint;

        [Header("Movement Settings")]
        [SerializeField] private float _baseSpeed = 2f;
        [SerializeField] private float _acceleratedSpeed = 4f;
        [SerializeField] private float _waypointReachThreshold = 0.15f;

        [Header("Stun Settings")]
        [SerializeField] private float _stunDuration = 2f;

        [Header("Acceleration Settings")]
        [SerializeField] private float _accelerationDuration = 3f;

        [Header("HP Settings")]
        [SerializeField] private float _maxHp = 100f;

        [Header("Virus Proximity / Panic")]
        [SerializeField] private float _panicRadius = 2.5f;
        [SerializeField] private float _proximityScanInterval = 0.3f;
        [SerializeField] private LayerMask _virusLayer;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        private RobotStateMachine _stateMachine;
        
        // BIẾN ĐỒNG BỘ MÁU: Tự động cập nhật giao diện khi máu thay đổi
        public NetworkVariable<float> currentHp = new NetworkVariable<float>(0f);

        private int _currentWaypointIndex = 0;
        private float _currentSpeed;
        private bool _isEscortComplete = false;
        private int _checkpointsFired = 0;
        private bool _escortGateOpen = true;

        private bool _isPanicked = false;
        private Coroutine _proximityCoroutine = null;

        private bool _isStunned = false;
        private bool _isAccelerating = false;
        private RobotState _stateBeforePanic = RobotState.Moving;

        private void Awake()
        {
            _stateMachine = GetComponent<RobotStateMachine>();
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            _currentSpeed = _baseSpeed;
            ValidateWaypointArrays();
        }

        public override void OnNetworkSpawn()
        {
            // Server nạp máu đầy
            if (IsServer) currentHp.Value = _maxHp;

            // Client và Server cùng lắng nghe khi máu bị trừ để cập nhật UI
            currentHp.OnValueChanged += (prev, current) => 
            {
                float normalizedHp = current / _maxHp;
                RobotEventBus.RaiseRobotDamaged(normalizedHp);
                UpdateEmotionFromHp(normalizedHp);

                // Nếu máu = 0, phát sự kiện chết
                if (current <= 0f && prev > 0f)
                {
                    _stateMachine.ChangeState(RobotState.Stunned);
                    RobotEventBus.RaiseRobotDied();
                }
            };
        }

        private void OnEnable()
        {
            QuizEventBus.OnQuizStarted += OnQuizStarted;
            QuizEventBus.OnQuizResolved += OnQuizResolved;
            QuizEventBus.OnTimerExpired += OnQuizTimerExpired;
        }

        private void OnDisable()
        {
            QuizEventBus.OnQuizStarted -= OnQuizStarted;
            QuizEventBus.OnQuizResolved -= OnQuizResolved;
            QuizEventBus.OnTimerExpired -= OnQuizTimerExpired;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            _stateMachine.ChangeState(RobotState.Moving);

            // CHỈ SERVER MỚI ĐƯỢC CHẠY QUÉT VIRUS
            if (IsServer)
            {
                _proximityCoroutine = StartCoroutine(ProximityScanLoop());
            }
        }

        private void Update()
        {
            // CHỈ SERVER MỚI ĐƯỢC TÍNH TOÁN DI CHUYỂN
            if (!IsServer || _isEscortComplete) return;
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (_isPanicked || !_escortGateOpen) return;

            RobotState state = _stateMachine.CurrentState;
            if (state != RobotState.Moving && state != RobotState.Accelerated) return;

            if (_currentWaypointIndex >= _waypoints.Length) return;

            Transform target = _waypoints[_currentWaypointIndex];
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * _currentSpeed * Time.deltaTime;

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= _waypointReachThreshold)
                OnWaypointReached(_currentWaypointIndex);
        }

        private void OnWaypointReached(int index)
        {
            transform.position = _waypoints[index].position;

            bool isFinal = (index >= _waypoints.Length - 1);
            bool isCheckpoint = index < _isCheckpoint.Length && _isCheckpoint[index];

            if (isFinal)
            {
                EscortCompleteClientRpc();
                return;
            }

            _currentWaypointIndex++;

            if (isCheckpoint)
            {
                _checkpointsFired++;
                CheckpointReachedClientRpc(index);
            }
        }

        // Báo cho mọi Client biết đã Checkpoint
        [ClientRpc]
        private void CheckpointReachedClientRpc(int index)
        {
            _stateMachine.ChangeState(RobotState.Waiting);
            RobotEventBus.RaiseCheckpointReached(index);
        }

        // Báo cho mọi Client biết đã Xong
        [ClientRpc]
        private void EscortCompleteClientRpc()
        {
            _isEscortComplete = true;
            _stateMachine.ChangeState(RobotState.Waiting);
            RobotEventBus.RaiseEscortComplete();
        }

        private void OnQuizStarted(QuizQuestion _) { Time.timeScale = 0f; }
        private void OnQuizResolved(bool _correct, int _index) { Time.timeScale = 1f; }
        private void OnQuizTimerExpired() { Time.timeScale = 1f; }

        private IEnumerator ProximityScanLoop()
        {
            var wait = new WaitForSecondsRealtime(_proximityScanInterval);
            while (!_isEscortComplete)
            {
                yield return wait;
                CheckVirusProximity();
            }
        }

        private void CheckVirusProximity()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _panicRadius, _virusLayer);
            bool enemyNearby = false;

            foreach (var hit in hits)
            {
                if (hit.GetComponent<VirusAI>() != null || hit.GetComponent<UtilityRobotAI_Floor3>() != null)
                {
                    enemyNearby = true; break;
                }
            }

            if (enemyNearby && !_isPanicked) EnterPanic();
            else if (!enemyNearby && _isPanicked) ExitPanic();
        }

        private void EnterPanic()
        {
            if (_isPanicked) return;
            var current = _stateMachine.CurrentState;
            _stateBeforePanic = (current == RobotState.Waiting) ? RobotState.Waiting : RobotState.Moving;
            
            // Gọi ClientRpc để đổi hiệu ứng hoảng loạn trên tất cả màn hình
            SyncPanicStateClientRpc(true);
        }

        private void ExitPanic()
        {
            // Gọi ClientRpc để đổi hiệu ứng bình thường trên tất cả màn hình
            SyncPanicStateClientRpc(false);
        }

        [ClientRpc]
        private void SyncPanicStateClientRpc(bool isPanicking)
        {
            _isPanicked = isPanicking;
            if (isPanicking)
            {
                _stateMachine.ChangeEmotion(RobotEmotion.Panicked);
                _stateMachine.ChangeState(RobotState.Panicked);
            }
            else
            {
                _stateMachine.ChangeEmotion(RobotEmotion.Stable);
                if (_isStunned)
                {
                    _stateMachine.ChangeState(RobotState.Stunned);
                    return;
                }
                if (_isAccelerating)
                {
                    _stateMachine.ChangeState(RobotState.Accelerated);
                    return;
                }
                RobotState resumeState = (_stateBeforePanic == RobotState.Waiting) ? RobotState.Waiting : RobotState.Moving;
                _stateMachine.ChangeState(resumeState);
            }
        }

        public void ResumeMovement()
        {
            if (!IsServer) return;
            _currentSpeed = _baseSpeed;
            SyncStateClientRpc(RobotState.Moving);
        }

        public void ApplySpeedBoost()
        {
            if (!IsServer) return;
            StartCoroutine(AccelerationCoroutine());
        }

        private IEnumerator AccelerationCoroutine()
        {
            _isAccelerating = true;
            _currentSpeed = _acceleratedSpeed;
            SyncStateClientRpc(RobotState.Accelerated);

            yield return new WaitForSeconds(_accelerationDuration);

            _isAccelerating = false;
            _currentSpeed = _baseSpeed;

            if (_isPanicked) SyncStateClientRpc(RobotState.Panicked);
            else if (_stateMachine.CurrentState == RobotState.Accelerated) SyncStateClientRpc(RobotState.Moving);
        }

        public void ApplyStun()
        {
            if (!IsServer) return;
            StartCoroutine(StunCoroutine());
        }

        private IEnumerator StunCoroutine()
        {
            _isStunned = true;
            SyncStateClientRpc(RobotState.Stunned);

            yield return new WaitForSeconds(_stunDuration);

            _isStunned = false;
            _currentSpeed = _baseSpeed;

            if (_isPanicked) SyncStateClientRpc(RobotState.Panicked);
            else SyncStateClientRpc(RobotState.Moving);
        }

        [ClientRpc]
        private void SyncStateClientRpc(RobotState newState)
        {
            _stateMachine.ChangeState(newState);
        }

        public void TakeDamage(float amount)
        {
            // Chỉ Server mới được tính toán sát thương
            if (!IsServer) return;
            
            // Trừ máu, NetworkVariable sẽ tự phát tín hiệu cho Client cập nhật giao diện
            currentHp.Value = Mathf.Clamp(currentHp.Value - amount, 0f, _maxHp);
        }

        private void UpdateEmotionFromHp(float normalizedHp)
        {
            if (_isPanicked) return;
            if (normalizedHp > 0.6f) _stateMachine.ChangeEmotion(RobotEmotion.Stable);
            else if (normalizedHp > 0.3f) _stateMachine.ChangeEmotion(RobotEmotion.Confused);
            else _stateMachine.ChangeEmotion(RobotEmotion.Panicked);
        }

        public float GetNormalizedHp() => currentHp.Value / _maxHp;
        public int GetCurrentWaypointIndex() => _currentWaypointIndex;
        public bool IsPanicked() => _isPanicked;

        public void SetEscortGate(bool open)
        {
            if (!IsServer) return;
            if (_escortGateOpen == open) return;
            _escortGateOpen = open;
        }

        private void ValidateWaypointArrays()
        {
            if (_waypoints == null) return;
            if (_isCheckpoint == null || _isCheckpoint.Length != _waypoints.Length)
                _isCheckpoint = new bool[_waypoints.Length];
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;
            Gizmos.color = _isPanicked ? Color.red : new Color(0.8f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _panicRadius);

            if (_waypoints == null) return;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;
                bool isCP = (_isCheckpoint != null && i < _isCheckpoint.Length && _isCheckpoint[i]);
                Gizmos.color = isCP ? Color.magenta : Color.cyan;
                Gizmos.DrawWireSphere(_waypoints[i].position, _waypointReachThreshold);
                if (isCP) Gizmos.DrawWireSphere(_waypoints[i].position, 0.4f);
                if (i + 1 < _waypoints.Length && _waypoints[i + 1] != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                }
            }

            if (Application.isPlaying && _currentWaypointIndex < _waypoints.Length && _waypoints[_currentWaypointIndex] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_waypoints[_currentWaypointIndex].position, 0.45f);
            }
        }
    }
}