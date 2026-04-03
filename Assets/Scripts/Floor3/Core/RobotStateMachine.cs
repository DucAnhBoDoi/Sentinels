// ============================================================
// FILE: Assets/Scripts/Floor3/Core/RobotStateMachine.cs
// Namespace: Scripts.Floor3.Core
// ------------------------------------------------------------
// Manages robot state transitions.
// WHY SEPARATE FROM RobotController:
//   - RobotController handles physics/movement
//   - RobotStateMachine handles logic/transitions
//   - Each can evolve independently
//   - Easier to unit-test state logic in isolation
// MULTIPLAYER NOTE: In multiplayer, only the HOST/SERVER should
//                   call ChangeState(). Clients observe via
//                   NetworkVariable<RobotState>.
// ============================================================

using UnityEngine;

namespace Scripts.Floor3.Core
{
    public class RobotStateMachine : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logTransitions = true;

        private RobotState _currentState = RobotState.Moving;
        private RobotEmotion _currentEmotion = RobotEmotion.Stable;

        public RobotState CurrentState => _currentState;
        public RobotEmotion CurrentEmotion => _currentEmotion;

        // ── State Transitions ────────────────────────────────────────────

        /// <summary>
        /// Request a state change. Validates transition legality.
        /// </summary>
        public void ChangeState(RobotState newState)
        {
            if (_currentState == newState) return;

            if (!IsTransitionValid(_currentState, newState))
            {
                Debug.LogWarning($"[RobotStateMachine] Invalid transition: {_currentState} → {newState}");
                return;
            }

            if (_logTransitions)
                Debug.Log($"[RobotStateMachine] {_currentState} → {newState}");

            _currentState = newState;
            RobotEventBus.RaiseStateChanged(_currentState);
        }

        /// <summary>
        /// Change robot emotion. No strict validation — emotions can shift freely.
        /// </summary>
        public void ChangeEmotion(RobotEmotion newEmotion)
        {
            if (_currentEmotion == newEmotion) return;
            _currentEmotion = newEmotion;
            RobotEventBus.RaiseEmotionChanged(_currentEmotion);
        }

        // ── Transition Validation ────────────────────────────────────────

        /// <summary>
        /// Defines legal state transitions.
        /// Add new transitions here as systems grow — never modify RobotController
        /// just to allow a transition.
        /// </summary>
        private bool IsTransitionValid(RobotState from, RobotState to)
        {
            // ── RULE: Panicked can be entered from ANY state ───────────────
            // Virus proximity check runs independently of quiz/stun logic.
            // Robot must always be able to panic regardless of current state.
            if (to == RobotState.Panicked) return true;

            // ── RULE: Panicked can exit to any movement state ─────────────
            if (from == RobotState.Panicked) return true;

            return (from, to) switch
            {
                // From Moving
                (RobotState.Moving, RobotState.Waiting) => true,
                (RobotState.Moving, RobotState.Stunned) => true,
                (RobotState.Moving, RobotState.Accelerated) => true,

                // From Waiting (at checkpoint)
                (RobotState.Waiting, RobotState.Moving) => true,
                (RobotState.Waiting, RobotState.Stunned) => true,
                (RobotState.Waiting, RobotState.Accelerated) => true,
                (RobotState.Waiting, RobotState.AskingQuestion) => true,

                // From AskingQuestion
                (RobotState.AskingQuestion, RobotState.Moving) => true,
                (RobotState.AskingQuestion, RobotState.Stunned) => true,
                (RobotState.AskingQuestion, RobotState.Accelerated) => true,

                // From Stunned (timer expires → resume)
                (RobotState.Stunned, RobotState.Moving) => true,

                // From Accelerated (boost ends)
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