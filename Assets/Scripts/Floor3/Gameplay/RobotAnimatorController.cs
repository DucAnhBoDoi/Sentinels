// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/RobotAnimatorController.cs
// Namespace: Scripts.Floor3.Gameplay
// ============================================================
// Lắng nghe RobotEventBus và RobotStateMachine để điều khiển
// Animator parameters. Hoàn toàn tách biệt khỏi RobotController.
//
// KHÔNG sửa RobotController — chỉ đọc events.
// Hoạt động đúng cả single player lẫn multiplayer vì
// ClientRpc đã sync state tới mọi client trước khi script này xử lý.
// ============================================================

using UnityEngine;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class RobotAnimatorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private RobotStateMachine _stateMachine;

        // Tên parameter phải khớp CHÍNH XÁC với Animator Controller
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");
        private static readonly int IsHurt = Animator.StringToHash("IsHurt");
        private static readonly int IsDead = Animator.StringToHash("IsDead");

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_stateMachine == null)
                _stateMachine = GetComponent<RobotStateMachine>();
        }

        private void OnEnable()
        {
            RobotEventBus.OnStateChanged += HandleStateChanged;
            RobotEventBus.OnRobotDamaged += HandleRobotDamaged;
            RobotEventBus.OnRobotDied += HandleRobotDied;
        }

        private void OnDisable()
        {
            RobotEventBus.OnStateChanged -= HandleStateChanged;
            RobotEventBus.OnRobotDamaged -= HandleRobotDamaged;
            RobotEventBus.OnRobotDied -= HandleRobotDied;
        }

        // ── Handlers ─────────────────────────────────────────────────────

        private void HandleStateChanged(RobotState newState)
        {
            switch (newState)
            {
                case RobotState.Moving:
                case RobotState.Accelerated:
                    _animator.SetBool(IsMoving, true);
                    break;

                case RobotState.Waiting:
                case RobotState.Stunned:
                case RobotState.Panicked:
                    _animator.SetBool(IsMoving, false);
                    break;
            }
        }

        private void HandleRobotDamaged(float normalizedHp)
        {
            // Chỉ trigger Hurt nếu robot chưa chết
            if (normalizedHp > 0f)
                _animator.SetTrigger(IsHurt);
        }

        private void HandleRobotDied()
        {
            _animator.SetBool(IsMoving, false);
            _animator.SetTrigger(IsDead);
        }
    }
}