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

        // ── Question Banks ────────────────────────────────────────────
        // Theme: Technology / Biology / Ethics
        // Replace or expand freely. Day 6 makes this obsolete.

        private readonly Dictionary<string, List<QuizQuestion>> _questionBanks =
            new Dictionary<string, List<QuizQuestion>>
        {
            {
                "technology",
                new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText       = "What does CPU stand for?",
                        Answers            = new[] { "Central Processing Unit", "Core Power Unit", "Computer Protocol Utility", "Central Program Upload" },
                        CorrectAnswerIndex = 0,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "Which protocol is used to assign IP addresses automatically?",
                        Answers            = new[] { "FTP", "DHCP", "DNS", "HTTP" },
                        CorrectAnswerIndex = 1,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What does RAM stand for?",
                        Answers            = new[] { "Read Access Memory", "Random Access Memory", "Rapid Array Module", "Reboot And Memory" },
                        CorrectAnswerIndex = 1,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "Which component converts AC power to DC for a computer?",
                        Answers            = new[] { "GPU", "Motherboard", "Power Supply Unit", "Heat Sink" },
                        CorrectAnswerIndex = 2,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What is the binary representation of the decimal number 5?",
                        Answers            = new[] { "011", "101", "110", "100" },
                        CorrectAnswerIndex = 1,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "Which layer of the OSI model handles IP addressing?",
                        Answers            = new[] { "Physical", "Data Link", "Network", "Transport" },
                        CorrectAnswerIndex = 2,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What does GPU stand for?",
                        Answers            = new[] { "General Processing Unit", "Graphical Protocol Utility", "Graphics Processing Unit", "Grid Power Unit" },
                        CorrectAnswerIndex = 2,
                        Topic              = "technology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What is the purpose of a firewall?",
                        Answers            = new[] { "Boost CPU speed", "Cool the processor", "Monitor and control network traffic", "Increase RAM" },
                        CorrectAnswerIndex = 2,
                        Topic              = "technology"
                    },
                }
            },

            {
                "biology",
                new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText       = "What is the powerhouse of the cell?",
                        Answers            = new[] { "Nucleus", "Ribosome", "Mitochondria", "Golgi apparatus" },
                        CorrectAnswerIndex = 2,
                        Topic              = "biology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "Which molecule stores genetic information?",
                        Answers            = new[] { "RNA", "ATP", "DNA", "Protein" },
                        CorrectAnswerIndex = 2,
                        Topic              = "biology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What process converts sunlight into energy?",
                        Answers            = new[] { "Respiration", "Photosynthesis", "Fermentation", "Mutation" },
                        CorrectAnswerIndex = 1,
                        Topic              = "biology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "How many chromosomes do humans have?",
                        Answers            = new[] { "23", "44", "46", "48" },
                        CorrectAnswerIndex = 2,
                        Topic              = "biology"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "Which blood type is universal donor?",
                        Answers            = new[] { "A+", "B-", "O-", "AB+" },
                        CorrectAnswerIndex = 2,
                        Topic              = "biology"
                    },
                }
            },

            {
                "ethics",
                new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        QuestionText       = "Which theory judges consequences?",
                        Answers            = new[] { "Deontology", "Virtue ethics", "Consequentialism", "Relativism" },
                        CorrectAnswerIndex = 2,
                        Topic              = "ethics"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What is autonomous AI?",
                        Answers            = new[] { "Human-controlled AI", "Self-operating AI", "Offline AI", "Mechanical AI" },
                        CorrectAnswerIndex = 1,
                        Topic              = "ethics"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "The trolley problem is what?",
                        Answers            = new[] { "Legal issue", "Moral dilemma", "Economic issue", "Political issue" },
                        CorrectAnswerIndex = 1,
                        Topic              = "ethics"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "What is data harvesting?",
                        Answers            = new[] { "Plant analysis", "Data collection", "File deletion", "Server cooling" },
                        CorrectAnswerIndex = 1,
                        Topic              = "ethics"
                    },
                    new QuizQuestion
                    {
                        QuestionText       = "AI ethics focuses on what?",
                        Answers            = new[] { "Machine speed", "Moral AI use", "Hardware design", "Game graphics" },
                        CorrectAnswerIndex = 1,
                        Topic              = "ethics"
                    },
                }
            }
        };

        private readonly HashSet<int> _usedIndices = new HashSet<int>();

        private string _currentTopic = "technology";

        // ── Public Topic Setter ──────────────────────────────────────

        public void SetTopic(string topic)
        {
            if (_questionBanks.ContainsKey(topic))
            {
                _currentTopic = topic;
                _usedIndices.Clear();

                Debug.Log($"[MockQuizGenerator] Topic set to: {_currentTopic}");
            }
            else
            {
                Debug.LogWarning($"[MockQuizGenerator] Unknown topic '{topic}', defaulting to technology.");
                _currentTopic = "technology";
            }
        }

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
            var bank = _questionBanks[_currentTopic];

            // Reset if all questions used
            if (_usedIndices.Count >= bank.Count)
            {
                _usedIndices.Clear();
                Debug.Log("[MockQuizGenerator] Question bank reset — cycling.");
            }

            // Find unused index
            int attempts = 0;
            while (attempts < 100)
            {
                int idx = UnityEngine.Random.Range(0, bank.Count);

                if (!_usedIndices.Contains(idx))
                {
                    _usedIndices.Add(idx);
                    return bank[idx];
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
            string[] shuffled = (string[])original.Answers.Clone();

            // Fisher-Yates shuffle
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int newCorrectIndex = Array.IndexOf(shuffled, correctText);

            return new QuizQuestion
            {
                QuestionText = original.QuestionText,
                Answers = shuffled,
                CorrectAnswerIndex = newCorrectIndex,
                Topic = original.Topic,
                TimeLimitOverride = original.TimeLimitOverride
            };
        }
    }
}