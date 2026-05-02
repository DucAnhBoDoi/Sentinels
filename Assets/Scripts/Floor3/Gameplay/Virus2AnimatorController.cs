// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/Virus2AnimatorController.cs
// Namespace: Scripts.Floor3.Gameplay
// ============================================================
// Điều khiển Animator của Virus2 (UtilityRobotAI_Floor3).
// Đọc trạng thái từ UtilityRobotAI_Floor3 mỗi frame.
// Không sửa UtilityRobotAI_Floor3.cs — hoàn toàn độc lập.
//
// LOGIC:
//   - IsMoving   = true khi virus đang di chuyển đến target/patrol
//   - IsAttacking= true khi virus trong stoppingDistance (đang cắn)
//   - IsHurt     = trigger khi TakeDamage được gọi (dùng event)
//   - IsDead     = trigger khi virus bị despawn
// ============================================================

using UnityEngine;
using Unity.Netcode;

namespace Scripts.Floor3.Gameplay
{
    [RequireComponent(typeof(Animator))]
    public class Virus2AnimatorController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private UtilityRobotAI_Floor3 _ai;
        [SerializeField] private Rigidbody2D _rb;

        [Header("Thresholds")]
        [Tooltip("Tốc độ tối thiểu để tính là đang moving")]
        [SerializeField] private float _moveThreshold = 0.1f;
        [Tooltip("Khớp với stoppingDistance trong UtilityRobotAI_Floor3")]
        [SerializeField] private float _attackThreshold = 1.5f;

        // Animator parameter hashes
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int IsHurt = Animator.StringToHash("IsHurt");
        private static readonly int IsDead = Animator.StringToHash("IsDead");

        private Animator _animator;
        private bool _isDead = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            if (_ai == null) _ai = GetComponent<UtilityRobotAI_Floor3>();
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (_isDead || _animator == null) return;

            // Animation chạy trên mọi client (visual only)
            UpdateMovementAnimation();
        }

        // ── Movement & Attack detection ───────────────────────────────────

        private void UpdateMovementAnimation()
        {
            if (_rb == null) return;

            float speed = _rb.linearVelocity.magnitude;
            bool isMoving = speed > _moveThreshold;

            // Xác định đang tấn công: velocity = 0 nhưng game đang chạy
            // (UtilityRobotAI set velocity = 0 khi trong stoppingDistance)
            bool isAttacking = !isMoving && _ai != null &&
                               HasTargetNearby();

            _animator.SetBool(IsMoving, isMoving && !isAttacking);
            _animator.SetBool(IsAttacking, isAttacking);
        }

        private bool HasTargetNearby()
        {
            if (_ai == null) return false;

            float distA = _ai.playerA != null
                ? Vector2.Distance(transform.position, _ai.playerA.position)
                : float.MaxValue;
            float distB = _ai.playerB != null
                ? Vector2.Distance(transform.position, _ai.playerB.position)
                : float.MaxValue;

            float closest = Mathf.Min(distA, distB);
            return closest <= _attackThreshold + 0.5f;
        }

        // ── Public API (gọi từ UtilityRobotAI_Floor3) ────────────────────

        /// <summary>
        /// Gọi khi virus bị đánh. Trigger Hurt animation trên mọi client.
        /// Gắn lời gọi này vào TriggerHitVisualClientRpc trong UtilityRobotAI_Floor3.
        /// </summary>
        public void TriggerHurt()
        {
            if (_isDead) return;
            _animator.SetTrigger(IsHurt);
        }

        /// <summary>
        /// Gọi trước khi Despawn. Trigger Death animation.
        /// </summary>
        public void TriggerDeath()
        {
            if (_isDead) return;
            _isDead = true;
            _animator.SetBool(IsMoving, false);
            _animator.SetBool(IsAttacking, false);
            _animator.SetTrigger(IsDead);
        }
    }
}