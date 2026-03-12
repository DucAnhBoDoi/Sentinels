// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/RobotController.cs
// Namespace: Scripts.Floor3.Gameplay
// ── UPDATED ────────────────────────────────────────────────
// NEW FEATURES:
//   1. QUIZ FREEZE: Time.timeScale = 0 when quiz opens,
//      Time.timeScale = 1 when quiz closes. Everything freezes —
//      viruses, players, robot, timers. Quiz UI still works
//      because it uses unscaled time (set on QuizManager timer).
//
//   2. VIRUS PROXIMITY PANIC: Robot scans nearby colliders
//      each second. If any VirusAI is within _panicRadius,
//      robot enters Panicked state and stops moving.
//      Resumes only when the area is clear.
//
// ARCHITECTURE NOTE on Time.timeScale:
//   Owned here because RobotController is the escort anchor.
//   QuizEventBus fires the events — RobotController reacts.
//   No other script needs to touch timeScale.
// ============================================================

using System.Collections;
using UnityEngine;
using Scripts.Floor3.Core;
using Scripts.Floor3.AI;

namespace Scripts.Floor3.Gameplay
{
    public class RobotController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Waypoints")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private bool[]      _isCheckpoint;

        [Header("Movement Settings")]
        [SerializeField] private float _baseSpeed            = 2f;
        [SerializeField] private float _acceleratedSpeed     = 4f;
        [SerializeField] private float _waypointReachThreshold = 0.15f;

        [Header("Stun Settings")]
        [SerializeField] private float _stunDuration = 2f;

        [Header("Acceleration Settings")]
        [SerializeField] private float _accelerationDuration = 3f;

        [Header("HP Settings")]
        [SerializeField] private float _maxHp = 100f;

        [Header("Virus Proximity / Panic")]
        [Tooltip("If any virus enters this radius, robot enters Panicked state and stops.")]
        [SerializeField] private float _panicRadius = 2.5f;

        [Tooltip("How often (seconds) to scan for nearby viruses. Lower = more responsive, more cost.")]
        [SerializeField] private float _proximityScanInterval = 0.3f;

        [Tooltip("Layer mask for virus objects. Set to the layer your virus prefab uses.")]
        [SerializeField] private LayerMask _virusLayer;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        // ── Private State ─────────────────────────────────────────────────

        private RobotStateMachine _stateMachine;

        private int   _currentWaypointIndex = 0;
        private float _currentHp;
        private float _currentSpeed;
        private bool  _isEscortComplete      = false;
        private int   _checkpointsFired      = 0;

        // Escort gate — set by ProximityDetector
        // Robot only moves when at least 1 player is close enough
        private bool      _escortGateOpen    = true;

        // Panic tracking
        private bool      _isPanicked        = false;
        private Coroutine _proximityCoroutine = null;

        // State before panic (so we can restore correctly)
        private RobotState _stateBeforePanic = RobotState.Moving;

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            _stateMachine = GetComponent<RobotStateMachine>();
            if (_stateMachine == null)
                Debug.LogError("[RobotController] Missing RobotStateMachine!");

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType    = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
            }

            _currentHp    = _maxHp;
            _currentSpeed = _baseSpeed;
            ValidateWaypointArrays();
        }

        private void OnEnable()
        {
            // ── FEATURE 1: Quiz Freeze ────────────────────────────────────
            // Subscribe to quiz events to freeze/unfreeze time
            QuizEventBus.OnQuizStarted  += OnQuizStarted;
            QuizEventBus.OnQuizResolved += OnQuizResolved;
            QuizEventBus.OnTimerExpired += OnQuizTimerExpired;
        }

        private void OnDisable()
        {
            QuizEventBus.OnQuizStarted  -= OnQuizStarted;
            QuizEventBus.OnQuizResolved -= OnQuizResolved;
            QuizEventBus.OnTimerExpired -= OnQuizTimerExpired;

            // Safety: always restore timescale if this object is disabled
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogError("[RobotController] No waypoints assigned!");
                return;
            }
            _stateMachine.ChangeState(RobotState.Moving);

            // Start proximity scan loop
            _proximityCoroutine = StartCoroutine(ProximityScanLoop());
        }

        // ── Update ───────────────────────────────────────────────────────

        private void Update()
        {
            if (_isEscortComplete) return;
            HandleMovement();
        }

        // ── Movement ─────────────────────────────────────────────────────

        private void HandleMovement()
        {
            // Guard 1: panic (virus nearby)
            if (_isPanicked) return;

            // Guard 2: escort gate — no player is close enough to escort
            // Robot waits politely until a player comes back
            if (!_escortGateOpen) return;

            RobotState state = _stateMachine.CurrentState;
            if (state != RobotState.Moving && state != RobotState.Accelerated)
                return;

            if (_currentWaypointIndex >= _waypoints.Length) return;

            Transform target    = _waypoints[_currentWaypointIndex];
            Vector3   direction = (target.position - transform.position).normalized;
            transform.position += direction * _currentSpeed * Time.deltaTime;

            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= _waypointReachThreshold)
                OnWaypointReached(_currentWaypointIndex);
        }

        private void OnWaypointReached(int index)
        {
            transform.position = _waypoints[index].position;

            bool isFinal      = (index >= _waypoints.Length - 1);
            bool isCheckpoint = index < _isCheckpoint.Length && _isCheckpoint[index];

            if (isFinal)
            {
                _isEscortComplete = true;
                _stateMachine.ChangeState(RobotState.Waiting);
                RobotEventBus.RaiseEscortComplete();
                return;
            }

            _currentWaypointIndex++;

            if (isCheckpoint)
            {
                _checkpointsFired++;
                Debug.Log($"[RobotController] Checkpoint #{_checkpointsFired} at waypoint {index}");
                _stateMachine.ChangeState(RobotState.Waiting);
                RobotEventBus.RaiseCheckpointReached(index);
            }
            else
            {
                Debug.Log($"[RobotController] Nav waypoint {index} → {_currentWaypointIndex}");
            }
        }

        // ── FEATURE 1: Quiz Time Freeze ───────────────────────────────────
        // WHY timeScale and not individual pause flags?
        //   timeScale = 0 pauses ALL Update/FixedUpdate/Coroutines globally.
        //   Viruses, players, animations, physics — all freeze instantly.
        //   Quiz UI uses Time.unscaledDeltaTime so its timer still works.
        //   This is the standard Unity approach for pause menus / quiz screens.

        private void OnQuizStarted(QuizQuestion _)
        {
            Debug.Log("[RobotController] Quiz opened → Freezing game (timeScale = 0).");
            Time.timeScale = 0f;
        }

        private void OnQuizResolved(bool _correct, int _index)
        {
            Debug.Log("[RobotController] Quiz closed → Resuming game (timeScale = 1).");
            Time.timeScale = 1f;
        }

        private void OnQuizTimerExpired()
        {
            // Timer expired also closes the quiz
            Time.timeScale = 1f;
        }

        // ── FEATURE 2: Virus Proximity / Panic ───────────────────────────
        // Scans a circle around the robot every _proximityScanInterval seconds.
        // If any VirusAI is found inside _panicRadius:
        //   → Robot enters Panicked state, stops moving
        //   → Emotion = Panicked
        // When scan finds zero viruses in range:
        //   → Robot resumes from Panicked

        private IEnumerator ProximityScanLoop()
        {
            // Use WaitForSecondsRealtime so scan runs even during timeScale = 0
            var wait = new WaitForSecondsRealtime(_proximityScanInterval);

            while (!_isEscortComplete)
            {
                yield return wait;
                CheckVirusProximity();
            }
        }

        private void CheckVirusProximity()
        {
            // OverlapCircle checks for VirusAI components within _panicRadius
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position, _panicRadius, _virusLayer);

            bool virusNearby = false;
            foreach (var hit in hits)
            {
                if (hit.GetComponent<VirusAI>() != null)
                {
                    virusNearby = true;
                    break;
                }
            }

            if (virusNearby && !_isPanicked)
            {
                EnterPanic();
            }
            else if (!virusNearby && _isPanicked)
            {
                ExitPanic();
            }
        }

        private void EnterPanic()
        {
            // Set flag FIRST — HandleMovement checks this flag directly.
            // Even if ChangeState fails below, the flag stops movement immediately.
            _stateBeforePanic = _stateMachine.CurrentState;
            _isPanicked       = true;

            _stateMachine.ChangeEmotion(RobotEmotion.Panicked);
            _stateMachine.ChangeState(RobotState.Panicked);  // requires updated RobotState.cs

            Debug.Log("[RobotController] Virus nearby — PANICKED. Robot stopped.");
        }

        private void ExitPanic()
        {
            _isPanicked = false;

            _stateMachine.ChangeEmotion(RobotEmotion.Stable);

            // Restore the appropriate state
            // If we were waiting at a checkpoint before panic, stay Waiting
            // Otherwise resume Moving
            RobotState resumeState = (_stateBeforePanic == RobotState.Waiting)
                ? RobotState.Waiting
                : RobotState.Moving;

            _stateMachine.ChangeState(resumeState);
            _currentSpeed = _baseSpeed;

            Debug.Log($"[RobotController] Area clear — exiting panic. Resuming: {resumeState}");
        }

        // ── Public API ────────────────────────────────────────────────────

        public void ResumeMovement()
        {
            _currentSpeed = _baseSpeed;
            _stateMachine.ChangeState(RobotState.Moving);
        }

        public void ApplySpeedBoost()
        {
            StartCoroutine(AccelerationCoroutine());
        }

        private IEnumerator AccelerationCoroutine()
        {
            _currentSpeed = _acceleratedSpeed;
            _stateMachine.ChangeState(RobotState.Moving);
            _stateMachine.ChangeState(RobotState.Accelerated);
            yield return new WaitForSeconds(_accelerationDuration);
            _currentSpeed = _baseSpeed;
            if (_stateMachine.CurrentState == RobotState.Accelerated)
                _stateMachine.ChangeState(RobotState.Moving);
        }

        public void ApplyStun()
        {
            StartCoroutine(StunCoroutine());
        }

        private IEnumerator StunCoroutine()
        {
            _stateMachine.ChangeState(RobotState.Stunned);
            yield return new WaitForSeconds(_stunDuration);
            _currentSpeed = _baseSpeed;
            // Only resume Moving if not currently in Panic
            if (!_isPanicked)
                _stateMachine.ChangeState(RobotState.Moving);
        }

        public void TakeDamage(float amount)
        {
            _currentHp = Mathf.Clamp(_currentHp - amount, 0f, _maxHp);
            float normalizedHp = _currentHp / _maxHp;
            RobotEventBus.RaiseRobotDamaged(normalizedHp);
            UpdateEmotionFromHp(normalizedHp);

            if (_currentHp <= 0f)
            {
                _stateMachine.ChangeState(RobotState.Stunned);
                RobotEventBus.RaiseRobotDied();
            }
        }

        // ── Emotion ──────────────────────────────────────────────────────

        private void UpdateEmotionFromHp(float normalizedHp)
        {
            // Don't override Panicked emotion — proximity check owns that
            if (_isPanicked) return;

            if      (normalizedHp > 0.6f) _stateMachine.ChangeEmotion(RobotEmotion.Stable);
            else if (normalizedHp > 0.3f) _stateMachine.ChangeEmotion(RobotEmotion.Confused);
            else                          _stateMachine.ChangeEmotion(RobotEmotion.Panicked);
        }

        // ── Getters ──────────────────────────────────────────────────────

        public float GetNormalizedHp()         => _currentHp / _maxHp;
        public int   GetCurrentWaypointIndex() => _currentWaypointIndex;
        public bool  IsPanicked()              => _isPanicked;

        /// <summary>
        /// Called by ProximityDetector.
        /// true  = at least 1 player is close → robot may move.
        /// false = both players are too far   → robot waits.
        /// </summary>
        public void SetEscortGate(bool open)
        {
            if (_escortGateOpen == open) return;
            _escortGateOpen = open;
            Debug.Log($"[RobotController] Escort gate: {(open ? "OPEN — moving" : "CLOSED — waiting for player")}");
        }

        // ── Validation ───────────────────────────────────────────────────

        private void ValidateWaypointArrays()
        {
            if (_waypoints == null) return;
            if (_isCheckpoint == null || _isCheckpoint.Length != _waypoints.Length)
            {
                Debug.LogWarning("[RobotController] _isCheckpoint resized.");
                _isCheckpoint = new bool[_waypoints.Length];
            }
        }

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            // Panic radius — red when panicked, dark red when normal
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

            if (Application.isPlaying && _currentWaypointIndex < _waypoints.Length
                && _waypoints[_currentWaypointIndex] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_waypoints[_currentWaypointIndex].position, 0.45f);
            }
        }
    }
}