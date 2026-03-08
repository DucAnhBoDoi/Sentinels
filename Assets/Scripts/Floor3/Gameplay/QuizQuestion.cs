// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/QuizQuestion.cs
// Namespace: Scripts.Floor3.Gameplay
// ------------------------------------------------------------
// Pure data model. No MonoBehaviour. No Unity dependencies.
// Used by ALL quiz generators (Mock, LLM, future network).
//
// WHY A PLAIN CLASS (not ScriptableObject)?
//   Questions are generated at runtime (Mock = random pick,
//   LLM = dynamic). ScriptableObjects are for authored,
//   design-time data. Runtime-generated data = plain class.
//
// MULTIPLAYER NOTE:
//   This class will be JSON-serialized and sent via
//   ClientRpc(questionJson) so all clients see the same question.
// ============================================================

using System;

namespace Scripts.Floor3.Gameplay
{
    [Serializable]
    public class QuizQuestion
    {
        public string   QuestionText;           // The question shown to players
        public string[] Answers;                // Always exactly 4 answers
        public int      CorrectAnswerIndex;     // 0–3
        public string   Topic;                  // e.g. "Networking", "Circuits" (for LLM context Day 6)
        public float    TimeLimitOverride;      // 0 = use DifficultyManager default

        // Convenience
        public string CorrectAnswerText => 
            (Answers != null && CorrectAnswerIndex < Answers.Length) 
                ? Answers[CorrectAnswerIndex] 
                : string.Empty;
    }
}
