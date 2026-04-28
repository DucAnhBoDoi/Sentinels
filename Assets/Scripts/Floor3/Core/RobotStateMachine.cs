// ============================================================
// FILE: Assets/Scripts/Floor3/Core/RobotStateMachine.cs
// Namespace: Scripts.Floor3.Core
// ------------------------------------------------------------
// Manages robot state transitions.
// MULTIPLAYER FIXED: Server determines state, Syncs to Clients via ClientRpc.
// ============================================================

using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG

namespace Scripts.Floor3.Core
{
    // ĐỔI TỪ MonoBehaviour SANG NetworkBehaviour
    public class RobotStateMachine : NetworkBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logTransitions = true;

        private RobotState _currentState = RobotState.Moving;
        private RobotEmotion _currentEmotion = RobotEmotion.Stable;

        public RobotState CurrentState => _currentState;
        public RobotEmotion CurrentEmotion => _currentEmotion;

        // ── State Transitions ────────────────────────────────────────────

        public void ChangeState(RobotState newState)
        {
            // BẢO VỆ: Chỉ Server mới có quyền thay đổi trạng thái gốc
            if (!IsServer) return;

            if (_currentState == newState) return;

            if (!IsTransitionValid(_currentState, newState))
            {
                Debug.LogWarning($"[RobotStateMachine] Invalid transition: {_currentState} → {newState}");
                return;
            }

            if (_logTransitions)
                Debug.Log($"[RobotStateMachine] {_currentState} → {newState}");

            // Áp dụng trên Server
            _currentState = newState;
            RobotEventBus.RaiseStateChanged(_currentState);

            // Bắn tín hiệu sang cho Client để cập nhật UI/Animation
            SyncStateClientRpc(newState);
        }

        [ClientRpc]
        private void SyncStateClientRpc(RobotState newState)
        {
            // Nếu là Server thì bỏ qua vì nó đã chạy lệnh ở trên rồi
            if (IsServer) return; 

            _currentState = newState;
            RobotEventBus.RaiseStateChanged(_currentState);
        }

        // ── Emotion Transitions ──────────────────────────────────────────

        public void ChangeEmotion(RobotEmotion newEmotion)
        {
            // BẢO VỆ: Chỉ Server mới có quyền quyết định cảm xúc
            if (!IsServer) return;

            if (_currentEmotion == newEmotion) return;
            
            // Áp dụng trên Server
            _currentEmotion = newEmotion;
            RobotEventBus.RaiseEmotionChanged(_currentEmotion);

            // Đồng bộ cảm xúc sang Client
            SyncEmotionClientRpc(newEmotion);
        }

        [ClientRpc]
        private void SyncEmotionClientRpc(RobotEmotion newEmotion)
        {
            if (IsServer) return;

            _currentEmotion = newEmotion;
            RobotEventBus.RaiseEmotionChanged(_currentEmotion);
        }

        // ── Transition Validation ────────────────────────────────────────

        private bool IsTransitionValid(RobotState from, RobotState to)
        {
            if (to == RobotState.Panicked) return true;
            if (from == RobotState.Panicked) return true;

            return (from, to) switch
            {
                (RobotState.Moving, RobotState.Waiting) => true,
                (RobotState.Moving, RobotState.Stunned) => true,
                (RobotState.Moving, RobotState.Accelerated) => true,

                (RobotState.Waiting, RobotState.Moving) => true,
                (RobotState.Waiting, RobotState.Stunned) => true,
                (RobotState.Waiting, RobotState.Accelerated) => true,
                (RobotState.Waiting, RobotState.AskingQuestion) => true,

                (RobotState.AskingQuestion, RobotState.Moving) => true,
                (RobotState.AskingQuestion, RobotState.Stunned) => true,
                (RobotState.AskingQuestion, RobotState.Accelerated) => true,

                (RobotState.Stunned, RobotState.Moving) => true,

                (RobotState.Accelerated, RobotState.Moving) => true,
                (RobotState.Accelerated, RobotState.Waiting) => true,

                _ => false
            };
        }

        // ── Convenience Helpers ──────────────────────────────────────────

        public bool IsMoving() => _currentState == RobotState.Moving;
        public bool IsStunned() => _currentState == RobotState.Stunned;
        public bool IsAccelerated() => _currentState == RobotState.Accelerated;
    }
}