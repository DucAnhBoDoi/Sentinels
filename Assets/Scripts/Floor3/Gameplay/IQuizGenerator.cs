// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/IQuizGenerator.cs
// Namespace: Scripts.Floor3.Gameplay
// ------------------------------------------------------------
// Strategy Pattern interface for quiz generation.
//
// WHY AN INTERFACE?
//   Day 2 = MockQuizGenerator (instant, offline)
//   Day 6 = LLMQuizGenerator  (async, calls Ollama)
//   QuizManager never changes — only the generator swaps.
//   This is the Open/Closed Principle in practice.
//
// The async signature (callbacks) works for BOTH:
//   - Mock: calls onComplete immediately (or next frame)
//   - LLM:  calls onComplete after HTTP response arrives
//
// MULTIPLAYER NOTE:
//   Only the SERVER calls RequestQuestion().
//   Result is broadcast to clients via ClientRpc.
// ============================================================

using System;

namespace Scripts.Floor3.Gameplay
{
    public interface IQuizGenerator
    {
        /// <summary>
        /// Request a question. Result delivered via callback (async-safe).
        /// </summary>
        /// <param name="waypointIndex">Which checkpoint triggered this quiz</param>
        /// <param name="onComplete">Called with the generated question</param>
        /// <param name="onError">Called if generation fails</param>
        void RequestQuestion(int waypointIndex, Action<QuizQuestion> onComplete, Action<string> onError);
    }
}
