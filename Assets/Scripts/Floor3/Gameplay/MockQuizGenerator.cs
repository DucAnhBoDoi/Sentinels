// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/MockQuizGenerator.cs
// Namespace: Scripts.Floor3.Gameplay
// ------------------------------------------------------------
// Offline quiz generator. Picks from a hardcoded bank.
// Implements IQuizGenerator — drop-in replaceable with LLM.
//
// WHY NOT ScriptableObject for the question bank?
//   These mock questions are TEMPORARY scaffolding.
//   Putting temp data in ScriptableObjects clutters the project.
//   Keep it here until the real generator exists.
//
// HOW TO USE:
//   QuizManager holds a reference to IQuizGenerator.
//   Swap MockQuizGenerator → LLMQuizGenerator in Inspector
//   with zero code changes to QuizManager.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Floor3.Gameplay
{
    public class MockQuizGenerator : MonoBehaviour, IQuizGenerator
    {
        [Header("Settings")]
        [Tooltip("Shuffle question order each time a question is picked")]
        [SerializeField] private bool _shuffleAnswers = true;

        // ── Question Bank ─────────────────────────────────────────────
        // Theme: Technology / Circuits / Networking (fits "Mechanical Soul" level)
        // Replace or expand freely. Day 6 makes this obsolete.

        private readonly List<QuizQuestion> _questionBank = new List<QuizQuestion>
        {
            new QuizQuestion
            {
                QuestionText       = "What does CPU stand for?",
                Answers            = new[] { "Central Processing Unit", "Core Power Unit", "Computer Protocol Utility", "Central Program Upload" },
                CorrectAnswerIndex = 0,
                Topic              = "Hardware"
            },
            new QuizQuestion
            {
                QuestionText       = "Which protocol is used to assign IP addresses automatically?",
                Answers            = new[] { "FTP", "DHCP", "DNS", "HTTP" },
                CorrectAnswerIndex = 1,
                Topic              = "Networking"
            },
            new QuizQuestion
            {
                QuestionText       = "What does RAM stand for?",
                Answers            = new[] { "Read Access Memory", "Random Access Memory", "Rapid Array Module", "Reboot And Memory" },
                CorrectAnswerIndex = 1,
                Topic              = "Hardware"
            },
            new QuizQuestion
            {
                QuestionText       = "Which component converts AC power to DC for a computer?",
                Answers            = new[] { "GPU", "Motherboard", "Power Supply Unit", "Heat Sink" },
                CorrectAnswerIndex = 2,
                Topic              = "Circuits"
            },
            new QuizQuestion
            {
                QuestionText       = "What is the binary representation of the decimal number 5?",
                Answers            = new[] { "011", "101", "110", "100" },
                CorrectAnswerIndex = 1,
                Topic              = "Logic"
            },
            new QuizQuestion
            {
                QuestionText       = "Which layer of the OSI model handles IP addressing?",
                Answers            = new[] { "Physical", "Data Link", "Network", "Transport" },
                CorrectAnswerIndex = 2,
                Topic              = "Networking"
            },
            new QuizQuestion
            {
                QuestionText       = "What does GPU stand for?",
                Answers            = new[] { "General Processing Unit", "Graphical Protocol Utility", "Graphics Processing Unit", "Grid Power Unit" },
                CorrectAnswerIndex = 2,
                Topic              = "Hardware"
            },
            new QuizQuestion
            {
                QuestionText       = "What is the purpose of a firewall?",
                Answers            = new[] { "Boost CPU speed", "Cool the processor", "Monitor and control network traffic", "Increase RAM" },
                CorrectAnswerIndex = 2,
                Topic              = "Networking"
            },
        };

        private readonly HashSet<int> _usedIndices = new HashSet<int>();

        // ── IQuizGenerator ────────────────────────────────────────────

        public void RequestQuestion(int waypointIndex, Action<QuizQuestion> onComplete, Action<string> onError)
        {
            QuizQuestion question = PickQuestion();

            if (question == null)
            {
                onError?.Invoke("[MockQuizGenerator] Question bank exhausted!");
                return;
            }

            if (_shuffleAnswers)
                question = ShuffleAnswers(question);

            // Simulate async (could use StartCoroutine with 0f delay for strict async)
            onComplete?.Invoke(question);
        }

        // ── Private Helpers ───────────────────────────────────────────

        private QuizQuestion PickQuestion()
        {
            // Reset if all questions used
            if (_usedIndices.Count >= _questionBank.Count)
            {
                _usedIndices.Clear();
                Debug.Log("[MockQuizGenerator] Question bank reset — cycling.");
            }

            // Find unused index
            int attempts = 0;
            while (attempts < 100)
            {
                int idx = UnityEngine.Random.Range(0, _questionBank.Count);
                if (!_usedIndices.Contains(idx))
                {
                    _usedIndices.Add(idx);
                    return _questionBank[idx];
                }
                attempts++;
            }

            return null;
        }

        /// <summary>
        /// Shuffles answer order and remaps CorrectAnswerIndex.
        /// Returns a NEW QuizQuestion — never mutates the bank.
        /// </summary>
        private QuizQuestion ShuffleAnswers(QuizQuestion original)
        {
            string correctText = original.CorrectAnswerText;
            string[] shuffled  = (string[])original.Answers.Clone();

            // Fisher-Yates shuffle
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int newCorrectIndex = Array.IndexOf(shuffled, correctText);

            return new QuizQuestion
            {
                QuestionText       = original.QuestionText,
                Answers            = shuffled,
                CorrectAnswerIndex = newCorrectIndex,
                Topic              = original.Topic,
                TimeLimitOverride  = original.TimeLimitOverride
            };
        }
    }
}
