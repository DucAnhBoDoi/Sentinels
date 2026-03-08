// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/QuizInputHandler.cs
// Namespace: Scripts.Floor3.Gameplay
// ------------------------------------------------------------
// Reads keyboard input ONLY during an active quiz.
// Fires into QuizEventBus — never talks to QuizManager directly.
//
// PLAYER A KEYS (WASD player):
//   1 = Answer slot 0
//   2 = Answer slot 1
//   3 = Answer slot 2
//   4 = Answer slot 3
//
// PLAYER B KEYS (Arrow player):
//   Numpad 1 = Answer slot 0
//   Numpad 2 = Answer slot 1
//   Numpad 3 = Answer slot 2
//   Numpad 4 = Answer slot 3
//
// WHY SEPARATE FROM PlayerMovement.cs?
//   - Separation of concerns: movement ≠ quiz input
//   - Quiz input is only active when quiz is active
//   - Can be replaced with UI buttons without touching PlayerMovement
//
// MULTIPLAYER NOTE:
//   Each client has its own QuizInputHandler.
//   It calls a [ServerRpc] instead of QuizEventBus directly.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class QuizInputHandler : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logInput = true;

        private bool _quizActive = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            QuizEventBus.OnQuizStarted  += OnQuizStarted;
            QuizEventBus.OnQuizResolved += OnQuizResolved;
            QuizEventBus.OnTimerExpired += OnTimerExpired;
        }

        private void OnDisable()
        {
            QuizEventBus.OnQuizStarted  -= OnQuizStarted;
            QuizEventBus.OnQuizResolved -= OnQuizResolved;
            QuizEventBus.OnTimerExpired -= OnTimerExpired;
        }

        private void OnQuizStarted(QuizQuestion _) => _quizActive = true;
        private void OnQuizResolved(bool _c, int _i) => _quizActive = false;
        private void OnTimerExpired() => _quizActive = false;

        // ── Input ────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_quizActive) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // ── Player A: keys 1–4 ───────────────────────────────────────
            if (kb.digit1Key.wasPressedThisFrame) SubmitA(0);
            if (kb.digit2Key.wasPressedThisFrame) SubmitA(1);
            if (kb.digit3Key.wasPressedThisFrame) SubmitA(2);
            if (kb.digit4Key.wasPressedThisFrame) SubmitA(3);

            // ── Player B: Numpad 1–4 ─────────────────────────────────────
            if (kb.numpad1Key.wasPressedThisFrame) SubmitB(0);
            if (kb.numpad2Key.wasPressedThisFrame) SubmitB(1);
            if (kb.numpad3Key.wasPressedThisFrame) SubmitB(2);
            if (kb.numpad4Key.wasPressedThisFrame) SubmitB(3);
        }

        private void SubmitA(int index)
        {
            if (_logInput) Debug.Log($"[QuizInputHandler] Player A pressed → Answer {index}");
            QuizEventBus.RaisePlayerAAnswered(index);
        }

        private void SubmitB(int index)
        {
            if (_logInput) Debug.Log($"[QuizInputHandler] Player B pressed → Answer {index}");
            QuizEventBus.RaisePlayerBAnswered(index);
        }
    }
}
