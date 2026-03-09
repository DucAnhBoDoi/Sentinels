// ============================================================
// FILE: Assets/Scripts/Floor3/AI/VirusAI.cs
// Namespace: Scripts.Floor3.AI
// ── REWRITTEN ──────────────────────────────────────────────
// KEY CHANGES:
//   - Wall avoidance via multi-directional raycasting (Context Steering)
//   - Virus navigates AROUND walls instead of through them
//   - Still no NavMesh — pure Physics2D raycast approach
//
// HOW WALL AVOIDANCE WORKS (Context Steering lite):
//   Each frame, cast N rays in a fan around the robot direction.
//   Rays that hit a wall layer get their weight reduced.
//   The final move direction is the weighted sum — naturally
//   steers away from walls while still heading toward robot.
//   Simple, performant, looks natural for swarm enemies.
//
// SETUP REQUIREMENT:
//   Walls must be on a dedicated Unity Layer (e.g. "Wall").
//   Set _wallLayer in Inspector to that layer.
//   Virus collider must NOT be a trigger if you want physical
//   wall blocking — use a separate small non-trigger collider
//   as the body, and a larger trigger for robot detection.
//
// MULTIPLAYER NOTE:
//   Movement runs server-side only.
//   NetworkTransform syncs position to clients.
// ============================================================

using System.Collections;
using UnityEngine;
using Scripts.ScriptableObjects;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.AI
{
    public class VirusAI : MonoBehaviour
    {
        // ── Internal State ────────────────────────────────────────────────

        private enum VirusState { Chasing, Attacking, Dead }

        // ── Injected Data ─────────────────────────────────────────────────

        private VirusData _data;
        private RobotController _robotTarget;

        // ── Inspector (set on prefab) ─────────────────────────────────────

        [Header("Wall Avoidance")]
        [Tooltip("Layer mask for walls. Viruses raycast against this to steer around obstacles.")]
        [SerializeField] private LayerMask _wallLayer;

        [Tooltip("How many rays to cast in the steering fan. More = smoother but more expensive.")]
        [SerializeField] private int _rayCount = 8;

        [Tooltip("How far each steering ray reaches.")]
        [SerializeField] private float _rayLength = 1.2f;

        [Tooltip("How strongly the virus avoids walls. Higher = wider berth around corners.")]
        [SerializeField] private float _avoidanceWeight = 2.5f;

        [Header("Debug")]
        [SerializeField] private bool _drawSteeringRays = false;

        // ── Private State ─────────────────────────────────────────────────

        private VirusState _state = VirusState.Chasing;
        private float _currentHp;
        private float _damageTimer = 0f;
        private bool _initialized = false;

        // ── Public Init ───────────────────────────────────────────────────

        public void Initialize(VirusData data, RobotController robotTarget)
        {
            _data = data;
            _robotTarget = robotTarget;
            _currentHp = data.MaxHp;
            _initialized = true;
        }

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Update()
        {
            if (!_initialized || _state == VirusState.Dead) return;

            if (_state == VirusState.Chasing) ChaseRobot();
            if (_state == VirusState.Attacking) TickDamage();
        }

        // ── Movement with Wall Avoidance ──────────────────────────────────

        private void ChaseRobot()
        {
            if (_robotTarget == null) return;

            Vector2 toRobot = ((Vector2)_robotTarget.transform.position - (Vector2)transform.position).normalized;
            Vector2 moveDir = ComputeSteeringDirection(toRobot);

            transform.position += (Vector3)(moveDir * _data.MoveSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Context Steering — casts rays in a circle, weights each direction.
        /// Directions toward walls get reduced weight.
        /// Result: smooth wall-avoiding movement toward the robot.
        /// </summary>
        private Vector2 ComputeSteeringDirection(Vector2 desiredDirection)
        {
            Vector2 bestDir = desiredDirection;
            float bestWeight = -1f;

            float angleStep = 360f / _rayCount;

            for (int i = 0; i < _rayCount; i++)
            {
                float angle = i * angleStep;
                Vector2 rayDir = RotateVector(desiredDirection, angle);

                // Base weight: how aligned is this ray with the robot direction?
                float weight = Vector2.Dot(rayDir, desiredDirection);

                // Cast ray — if wall hit, penalize this direction
                RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDir, _rayLength, _wallLayer);
                if (hit.collider != null)
                {
                    // Scale penalty by proximity — closer wall = stronger avoidance
                    float proximity = 1f - (hit.distance / _rayLength);
                    weight -= _avoidanceWeight * proximity;
                }

                if (_drawSteeringRays)
                {
                    Color c = (hit.collider != null) ? Color.red : Color.green;
                    Debug.DrawRay(transform.position, rayDir * _rayLength, c);
                }

                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    bestDir = rayDir;
                }
            }

            return bestDir.normalized;
        }

        private static Vector2 RotateVector(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }

        // ── Damage Tick ───────────────────────────────────────────────────

        private void TickDamage()
        {
            _damageTimer -= Time.deltaTime;
            if (_damageTimer <= 0f)
            {
                _robotTarget?.TakeDamage(_data.DamageOnContact);
                _damageTimer = _data.DamageCooldown;
            }
        }

        // ── Collision ─────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state == VirusState.Dead) return;
            if (other.GetComponent<RobotController>() != null)
            {
                _state = VirusState.Attacking;
                _damageTimer = 0f;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<RobotController>() != null)
                if (_state == VirusState.Attacking)
                    _state = VirusState.Chasing;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (_state == VirusState.Dead) return;
            _currentHp -= amount;
            if (_currentHp <= 0f) Die();
        }

        // ── Death ─────────────────────────────────────────────────────────

        private void Die()
        {
            if (_state == VirusState.Dead) return;
            _state = VirusState.Dead;
            StartCoroutine(DeathCoroutine());
        }

        private IEnumerator DeathCoroutine()
        {
            yield return new WaitForSeconds(0.15f);
            Destroy(gameObject);
        }
    }
}