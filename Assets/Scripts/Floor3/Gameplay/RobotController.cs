// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/RobotController.cs
// Namespace: Scripts.Floor3.Gameplay
// ============================================================
// KEY ARCHITECTURE DECISION — Why Kinematic + transform.position:
//
//   Dynamic Rigidbody2D (old approach):
//     - Receives collision forces from players/enemies
//     - MovePosition() still lets physics engine nudge the body
//     - FreezePosition conflicts with MovePosition (Unity removes it at runtime)
//     - velocity zeroing fights physics engine every frame = jitter
//
//   Kinematic Rigidbody2D (new approach):
//     - Physics engine NEVER applies forces to it — immune by design
//     - Players collide WITH it (collider still active) but cannot move it
//     - We control position 100% via transform.position in Update()
//     - No conflicts, no jitter, no runtime constraint toggling
//     - Still has Collider2D so enemies/players detect proximity correctly
//
// MULTIPLAYER NOTE:
//   Replace transform.position with NetworkTransform or
//   ClientNetworkTransform. Movement logic stays identical.
// ============================================================

using System.Collections;
using UnityEngine;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class RobotController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Waypoints")]
        [Tooltip("All navigation waypoints — corners, turns, etc.")]
        [SerializeField] private Transform[] _waypoints;

        [Tooltip("Mark TRUE for waypoints that should trigger a quiz checkpoint.\n" +
                 "Must be the same length as _waypoints array.")]
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

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        // ── Private State ────────────────────────────────────────────────

        private RobotStateMachine _stateMachine;

        private int   _currentWaypointIndex = 0;
        private float _currentHp;
        private float _currentSpeed;
        private bool  _isEscortComplete = false;
        private int   _checkpointsFired = 0;

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            _stateMachine = GetComponent<RobotStateMachine>();
            if (_stateMachine == null)
                Debug.LogError("[RobotController] Missing RobotStateMachine component!");

            // ── CRITICAL: Force Kinematic at runtime ──────────────────────
            // Even if someone sets Dynamic in Inspector, we override it here.
            // Kinematic = physics engine never applies forces to this body.
            // The robot is physically present (collider works) but unmovable
            // by any external physics — players bounce off it, robot stays put.
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType    = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
                Debug.Log("[RobotController] Rigidbody2D set to Kinematic — immune to external physics.");
            }

            _currentHp    = _maxHp;
            _currentSpeed = _baseSpeed;

            ValidateWaypointArrays();
        }

        private void Start()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                Debug.LogError("[RobotController] No waypoints assigned!");
                return;
            }
            _stateMachine.ChangeState(RobotState.Moving);
        }

        // ── Use Update (not FixedUpdate) for transform-based movement ────
        // FixedUpdate is for Rigidbody physics.
        // transform.position is not physics — use Update for smooth results.
        private void Update()
        {
            if (_isEscortComplete) return;
            HandleMovement();
        }

        // ── Movement ─────────────────────────────────────────────────────

        private void HandleMovement()
        {
            RobotState state = _stateMachine.CurrentState;
            if (state != RobotState.Moving && state != RobotState.Accelerated)
                return;

            if (_currentWaypointIndex >= _waypoints.Length) return;

            Transform target    = _waypoints[_currentWaypointIndex];
            Vector3   direction = (target.position - transform.position).normalized;

            // Move directly via transform — no physics involved
            transform.position += direction * _currentSpeed * Time.deltaTime;

            // Check arrival
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= _waypointReachThreshold)
                OnWaypointReached(_currentWaypointIndex);
        }

        private void OnWaypointReached(int index)
        {
            // Snap exactly to waypoint to prevent threshold overshoot issues
            transform.position = _waypoints[index].position;

            bool isFinalWaypoint  = (index >= _waypoints.Length - 1);
            bool isCheckpointHere = index < _isCheckpoint.Length && _isCheckpoint[index];

            if (isFinalWaypoint)
            {
                _isEscortComplete = true;
                _stateMachine.ChangeState(RobotState.Waiting);
                RobotEventBus.RaiseEscortComplete();
                return;
            }

            _currentWaypointIndex++;

            if (isCheckpointHere)
            {
                _checkpointsFired++;
                Debug.Log($"[RobotController] Quiz Checkpoint #{_checkpointsFired} at waypoint {index}");
                _stateMachine.ChangeState(RobotState.Waiting);
                RobotEventBus.RaiseCheckpointReached(index);
            }
            else
            {
                Debug.Log($"[RobotController] Nav waypoint {index} → next: {_currentWaypointIndex}");
            }
        }

        // ── Public API ───────────────────────────────────────────────────

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
            _stateMachine.ChangeState(RobotState.Moving);       // Waiting → Moving
            _stateMachine.ChangeState(RobotState.Accelerated);  // Moving  → Accelerated
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
            if      (normalizedHp > 0.6f) _stateMachine.ChangeEmotion(RobotEmotion.Stable);
            else if (normalizedHp > 0.3f) _stateMachine.ChangeEmotion(RobotEmotion.Confused);
            else                          _stateMachine.ChangeEmotion(RobotEmotion.Panicked);
        }

        // ── Getters ──────────────────────────────────────────────────────

        public float GetNormalizedHp()         => _currentHp / _maxHp;
        public int   GetCurrentWaypointIndex() => _currentWaypointIndex;

        // ── Validation ───────────────────────────────────────────────────

        private void ValidateWaypointArrays()
        {
            if (_waypoints == null) return;
            if (_isCheckpoint == null || _isCheckpoint.Length != _waypoints.Length)
            {
                Debug.LogWarning("[RobotController] _isCheckpoint array resized. Configure in Inspector.");
                _isCheckpoint = new bool[_waypoints.Length];
            }
        }

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _waypoints == null) return;

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