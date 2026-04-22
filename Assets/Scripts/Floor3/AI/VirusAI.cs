using System.Collections;
using UnityEngine;
using Scripts.ScriptableObjects;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.AI
{
    // THÊM IDamagable VÀO ĐÂY ĐỂ PLAYER CỦA ANH CHÉM ĐƯỢC
    public class VirusAI : MonoBehaviour, IDamagable
    {
        // ── Internal State ────────────────────────────────────────────────
        private enum VirusState { Chasing, Attacking, Dead }

        // ── Injected Data ─────────────────────────────────────────────────
        private VirusData _data;
        private RobotController _robotTarget;

        // ── Inspector (set on prefab) ─────────────────────────────────────
        [Header("Wall Avoidance")]
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private int _rayCount = 8;
        [SerializeField] private float _rayLength = 1.2f;
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
            if (!Scripts.Floor3.UI.TopicSelectionUI.hasStartedMission) return;

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

        private Vector2 ComputeSteeringDirection(Vector2 desiredDirection)
        {
            Vector2 bestDir = desiredDirection;
            float bestWeight = -1f;

            float angleStep = 360f / _rayCount;

            for (int i = 0; i < _rayCount; i++)
            {
                float angle = i * angleStep;
                Vector2 rayDir = RotateVector(desiredDirection, angle);

                float weight = Vector2.Dot(rayDir, desiredDirection);

                RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDir, _rayLength, _wallLayer);
                if (hit.collider != null)
                {
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
                _robotTarget?.TakeDamage(_data.DamageOnContact); // Chỉ cắn Robot
                _damageTimer = _data.DamageCooldown;
            }
        }

        // ── Collision ─────────────────────────────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_state == VirusState.Dead) return;
            // Chỉ bắt sự kiện chạm vào Robot
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

        // ── HÀM NHẬN ĐÒN TỪ HỆ THỐNG PLAYER CỦA ANH (MỚI THÊM) ─────────────
        public void TakeDamage()
        {
            // PlayerMovement của anh gọi hàm này (không có tham số)
            // Em mặc định mỗi cú chém gây 1 sát thương
            TakeDamage(1f);
        }

        // ── Public API (Hàm gốc của bạn anh, CẦN GIỮ NGUYÊN) ──────────────
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