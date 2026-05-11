// ============================================================
// FILE: Assets/Scripts/Floor3/AI/QuizDataParser.cs
// Namespace: Scripts.Floor3.AI
// ───────────────────────────────────────────────────
// Parses the JSON response from the Vercel backend into
// a List<QuizQuestion> that QuizManager can use directly.
//
// BACKEND JSON FORMAT:
// {
//   "topic": "technology",
//   "source": "gemini",
//   "quizzes": [
//     {
//       "question": "...",
//       "answers": ["A","B","C","D"],
//       "correctIndex": 1
//     }
//   ]
// }
//
// UNITY JSON NOTE:
//   Unity's JsonUtility requires [Serializable] wrapper classes.
//   It does NOT support root-level arrays or polymorphic types.
//   We use JsonUtility for simplicity — no extra packages needed.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.AI
{
    // ── JSON Wrapper classes (match backend schema exactly) ───────

    [Serializable]
    public class BackendQuizResponse
    {
        public string            topic;
        public string            source;   // "gemini" or "fallback"
        public BackendQuizItem[] quizzes;
    }

    [Serializable]
    public class BackendQuizItem
    {
        public string   question;
        public string[] answers;
        public int      correctIndex;
    }

    // ── Parser ────────────────────────────────────────────────────

    public static class QuizDataParser
    {
        /// <summary>
        /// Parse raw JSON string from backend into a list of QuizQuestion.
        /// Returns null if parsing fails — caller must handle null.
        /// </summary>
        public static List<QuizQuestion> Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[QuizDataParser] Received empty JSON string.");
                return null;
            }

            BackendQuizResponse response;
            try
            {
                response = JsonUtility.FromJson<BackendQuizResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[QuizDataParser] JSON parse failed: {e.Message}\nRaw: {json}");
                return null;
            }

            if (response == null || response.quizzes == null || response.quizzes.Length == 0)
            {
                Debug.LogError("[QuizDataParser] Parsed response has no quizzes.");
                return null;
            }

            var questions = new List<QuizQuestion>();

            foreach (var item in response.quizzes)
            {
                // Validate each item before converting
                if (string.IsNullOrEmpty(item.question))
                {
                    Debug.LogWarning("[QuizDataParser] Skipping quiz item — empty question.");
                    continue;
                }

                if (item.answers == null || item.answers.Length != 4)
                {
                    Debug.LogWarning($"[QuizDataParser] Skipping '{item.question}' — must have 4 answers.");
                    continue;
                }

                if (item.correctIndex < 0 || item.correctIndex > 3)
                {
                    Debug.LogWarning($"[QuizDataParser] Skipping '{item.question}' — invalid correctIndex.");
                    continue;
                }

                questions.Add(new QuizQuestion
                {
                    QuestionText       = item.question,
                    Answers            = item.answers,
                    CorrectAnswerIndex = item.correctIndex,
                    Topic              = response.topic,
                    TimeLimitOverride  = 0f   // DifficultyManager controls time
                });
            }

            Debug.Log($"[QuizDataParser] Parsed {questions.Count} questions. " +
                      $"Topic: {response.topic}, Source: {response.source}");

            return questions.Count > 0 ? questions : null;
        }
    }
}
