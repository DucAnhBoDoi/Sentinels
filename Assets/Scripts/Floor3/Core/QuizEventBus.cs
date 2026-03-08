// ============================================================
// FILE: Assets/Scripts/Floor3/Core/QuizEventBus.cs
// Namespace: Scripts.Floor3.Core
// ------------------------------------------------------------
// Event bus for all quiz-related events.
// Mirrors RobotEventBus pattern — same architecture, same rules.
//
// Flow:
//   QuizManager  ──raises──▶  QuizEventBus  ──notifies──▶  QuizHUDController
//   UI Input     ──raises──▶  QuizEventBus  ──notifies──▶  QuizManager
//
// This means QuizManager and QuizHUDController NEVER reference each other.
//
// MULTIPLAYER NOTE:
//   OnPlayerAAnswered / OnPlayerBAnswered will be replaced by ServerRpc calls.
//   OnQuizStarted will be replaced by ClientRpc to sync UI on all clients.
// ============================================================

using System;
using UnityEngine;

namespace Scripts.Floor3.Core
{
    // Which player slot submitted an answer
    public enum PlayerSlot { PlayerA, PlayerB }

    public static class QuizEventBus
    {
        // ── QuizManager → UI ──────────────────────────────────────────────

        // Quiz begins — carries the full question data
        public static event Action<Scripts.Floor3.Gameplay.QuizQuestion> OnQuizStarted;

        // Timer tick every frame — normalized (1→0) and seconds remaining
        public static event Action<float, float> OnTimerTick;

        // Timer ran out
        public static event Action OnTimerExpired;

        // A player locked in their answer
        public static event Action<PlayerSlot, int> OnPlayerConfirmed;

        // Both players answered but disagreed
        public static event Action<int, int> OnConflictDetected;    // (answerA, answerB)

        // Quiz resolved with result
        public static event Action<bool, int> OnQuizResolved;       // (isCorrect, correctIndex)

        // ── UI → QuizManager ─────────────────────────────────────────────

        // Player A submits answer index (0–3)
        public static event Action<int> OnPlayerAAnswered;

        // Player B submits answer index (0–3)
        public static event Action<int> OnPlayerBAnswered;

        // ── Invokers (called only by QuizManager or UI input handlers) ───

        public static void RaiseQuizStarted(Scripts.Floor3.Gameplay.QuizQuestion q)
        {
            Debug.Log($"[QuizEventBus] Quiz started: \"{q.QuestionText}\"");
            OnQuizStarted?.Invoke(q);
        }

        public static void RaiseTimerTick(float normalized, float secondsLeft)
            => OnTimerTick?.Invoke(normalized, secondsLeft);

        public static void RaiseTimerExpired()
        {
            Debug.Log("[QuizEventBus] Timer expired.");
            OnTimerExpired?.Invoke();
        }

        public static void RaisePlayerConfirmed(PlayerSlot slot, int answerIndex)
        {
            Debug.Log($"[QuizEventBus] {slot} confirmed answer {answerIndex}");
            OnPlayerConfirmed?.Invoke(slot, answerIndex);
        }

        public static void RaiseConflictDetected(int answerA, int answerB)
        {
            Debug.Log($"[QuizEventBus] Conflict! A={answerA} B={answerB}");
            OnConflictDetected?.Invoke(answerA, answerB);
        }

        public static void RaiseQuizResolved(bool isCorrect, int correctIndex)
        {
            Debug.Log($"[QuizEventBus] Quiz resolved. Correct={isCorrect}, CorrectIndex={correctIndex}");
            OnQuizResolved?.Invoke(isCorrect, correctIndex);
        }

        // Called by UI button presses / keyboard input handlers
        public static void RaisePlayerAAnswered(int answerIndex)
            => OnPlayerAAnswered?.Invoke(answerIndex);

        public static void RaisePlayerBAnswered(int answerIndex)
            => OnPlayerBAnswered?.Invoke(answerIndex);
    }
}
