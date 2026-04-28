using System.Collections;
using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG
using Scripts.ScriptableObjects;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.AI
{
    // ĐỔI SANG NetworkBehaviour
    public class VirusAI : NetworkBehaviour, IDamagable
    {
        private enum VirusState { Chasing, Attacking, Dead }

        private VirusData _data;
        private RobotController _robotTarget;

        [Header("Wall Avoidance")]
        [SerializeField] private LayerMask _wallLayer;
        [SerializeField] private int _rayCount = 8;
        [SerializeField] private float _rayLength = 1.2f;
        [SerializeField] private float _avoidanceWeight = 2.5f;

        // ĐÃ XÓA BIẾN_drawSteeringRays Ở ĐÂY ĐỂ TRÁNH LỖI CS0414

        private VirusState _state = VirusState.Chasing;
        private float _currentHp;
        private float _damageTimer = 0f;
        private bool _initialized = false;
        private HitReactionController _hitReaction;

        public void Initialize(VirusData data, RobotController robotTarget)
        {
            _data = data;
            _robotTarget = robotTarget;
            _currentHp = data.MaxHp;
            _initialized = true;
            _hitReaction = GetComponent<HitReactionController>();
        }

        private void Update()
        {
            // CHỈ SERVER MỚI ĐƯỢC CHẠY TRÍ TUỆ NHÂN TẠO
            if (!IsServer) return;

            if (!Scripts.Floor3.UI.TopicSelectionUI.hasStartedMission) return;
            if (!_initialized || _state == VirusState.Dead) return;

            if (_hitReaction != null && _hitReaction.IsBeingKnockedBack) return;

            if (_state == VirusState.Chasing) ChaseRobot();
            if (_state == VirusState.Attacking) TickDamage();
        }

        private void ChaseRobot()
        {
            if (_robotTarget == null) return;
            Vector2 toRobot = ((Vector2)_robotTarget.transform.position - (Vector2)transform.position).normalized;
            Vector2 moveDir = ComputeSteeringDirection(toRobot);
            transform.position += (Vector3)(moveDir * _data.MoveSpeed * Time.deltaTime);
        }

        private Vector2 ComputeSteeringDirection(Vector2 desiredDirection)
        {
            Vector2 bestDir = desiredDirection; float bestWeight = -1f;
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
                
                if (weight > bestWeight) { bestWeight = weight; bestDir = rayDir; }
            }
            return bestDir.normalized;
        }

        private static Vector2 RotateVector(Vector2 v, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad); float sin = Mathf.Sin(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }

        private void TickDamage()
        {
            _damageTimer -= Time.deltaTime;
            if (_damageTimer <= 0f)
            {
                _robotTarget?.TakeDamage(_data.DamageOnContact); 
                _damageTimer = _data.DamageCooldown;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || _state == VirusState.Dead) return;
            if (other.GetComponent<RobotController>() != null)
            {
                _state = VirusState.Attacking;
                _damageTimer = 0f;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsServer) return;
            if (other.GetComponent<RobotController>() != null && _state == VirusState.Attacking)
                _state = VirusState.Chasing;
        }

        public void TakeDamage() { TakeDamage(1f); }

        public void TakeDamage(float amount)
        {
            // CHỈ SERVER NHẬN LỆNH TRỪ MÁU
            if (!IsServer || _state == VirusState.Dead) return;

            if (_hitReaction != null)
            {
                Vector2 knockbackDir = Vector2.up;
                if (_robotTarget != null)
                    knockbackDir = ((Vector2)transform.position - (Vector2)_robotTarget.transform.position).normalized;
                
                _hitReaction.ReactOnly(knockbackDir);
                TriggerHitVisualClientRpc(knockbackDir); // Chớp trắng trên máy Client
            }

            _currentHp -= amount;
            if (_currentHp <= 0f) Die();
        }

        [ClientRpc]
        private void TriggerHitVisualClientRpc(Vector2 dir)
        {
            if (!IsServer && _hitReaction != null) _hitReaction.ReactOnly(dir);
        }

        private void Die()
        {
            if (_state == VirusState.Dead) return;
            _state = VirusState.Dead;
            StartCoroutine(DeathCoroutine());
        }

        private IEnumerator DeathCoroutine()
        {
            yield return new WaitForSeconds(0.15f);
            if (NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        }
    }
}