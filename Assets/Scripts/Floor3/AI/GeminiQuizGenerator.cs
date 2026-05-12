// ============================================================
// FILE: Assets/Scripts/Floor3/AI/GeminiQuizGenerator.cs
// Namespace: Scripts.Floor3.AI
// ───────────────────────────────────────────────────
// Implements IQuizGenerator using a PRELOADED question queue.
//
// KEY DESIGN DECISION — Generate Once, Not Per Checkpoint:
//   OLD approach (MockQuizGenerator): RequestQuestion() calls
//   the generator each time a checkpoint is reached.
//
//   NEW approach (GeminiQuizGenerator):
//   - FetchAndPreload() is called ONCE at level start (by TopicSelectionUI)
//   - Backend returns 5 questions → stored in a Queue<QuizQuestion>
//   - RequestQuestion() simply Dequeue()s the next question
//   - No network call during gameplay = zero latency at checkpoints
//
// FALLBACK CHAIN:
//   Backend OK         → use Gemini questions
//   Backend fail/timeout → use MockQuizGenerator questions
//   Both fail          → robot auto-resumes (Floor3Brain handles)
//
// SWAP INSTRUCTION:
//   On Quiz_Manager GameObject:
//   - Remove: MockQuizGenerator component
//   - Add:    GeminiQuizGenerator component
//   - Add:    GeminiAPIService component (same GameObject)
//   - Fill:   Backend URL in GeminiAPIService Inspector
//   QuizManager detects IQuizGenerator automatically via GetComponent.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.AI
{
    public class GeminiQuizGenerator : MonoBehaviour, IQuizGenerator
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("References")]
        [Tooltip("Must be on the same GameObject as this script.")]
        [SerializeField] private GeminiAPIService _apiService;

        [Header("Fallback")]
        [Tooltip("Used when backend fails. Same MockQuizGenerator already in project.")]
        [SerializeField] private MockQuizGenerator _mockFallback;

        [Header("Debug")]
        [SerializeField] private bool _logFlow = true;

        // ── State ─────────────────────────────────────────────────────────

        private Queue<QuizQuestion> _questionQueue = new Queue<QuizQuestion>();
        private bool _isReady = false;
        private bool _usingFallback = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_apiService == null)
                _apiService = GetComponent<GeminiAPIService>();

            if (_apiService == null)
                Debug.LogError("[GeminiQuizGenerator] GeminiAPIService not found on this GameObject!");
        }

        // ── Public Preload API (called by TopicSelectionUI) ───────────────

        /// <summary>
        /// Called ONCE at level start after player selects a topic.
        /// Fetches 5 questions from backend and stores them in the queue.
        /// onReady: called when questions are ready (success or fallback)
        /// </summary>
        public void FetchAndPreload(string topic, Action onReady)
        {
            _isReady = false;
            _usingFallback = false;
            _questionQueue.Clear();

            Log($"Fetching questions for topic: {topic}");

            _apiService.FetchQuizzes(
                topic,
                onSuccess: questions =>
                {
                    foreach (var q in questions)
                        _questionQueue.Enqueue(q);

                    _isReady = true;
                    Log($"✓ Preloaded {_questionQueue.Count} questions from backend.");
                    onReady?.Invoke();
                },
                onFailure: error =>
                {
                    Log($"⚠ Backend failed ({error}) → loading MockQuizGenerator fallback.");
                    LoadMockFallback(topic);
                    _isReady = true;
                    onReady?.Invoke();
                }
            );
        }

        // ── IQuizGenerator ────────────────────────────────────────────────

        /// <summary>
        /// Called by QuizManager at each checkpoint.
        /// Dequeues the next preloaded question — instant, no network call.
        /// </summary>
        public void RequestQuestion(
            int waypointIndex,
            Action<QuizQuestion> onComplete,
            Action<string> onError)
        {
            if (!_isReady)
            {
                onError?.Invoke("Questions not preloaded yet. Call FetchAndPreload() first.");
                return;
            }

            // If queue is empty, cycle back (more checkpoints than questions)
            if (_questionQueue.Count == 0)
            {
                Log("Queue exhausted — refilling from fallback.");
                LoadMockFallback("technology");
            }

            var question = _questionQueue.Dequeue();

            Log($"Serving question {waypointIndex + 1}: \"{question.QuestionText}\"" +
                (_usingFallback ? " [FALLBACK]" : " [GEMINI]"));

            onComplete?.Invoke(question);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void LoadMockFallback(string topic)
        {
            _usingFallback = true;

            if (_mockFallback == null)
            {
                Debug.LogError("[GeminiQuizGenerator] No MockQuizGenerator assigned for fallback!");
                return;
            }

            _mockFallback.SetTopic(topic);

            // Pull questions from Mock into our queue
            // We request 5 times to fill the queue
            int loaded = 0;

            for (int i = 0; i < 5; i++)
            {
                _mockFallback.RequestQuestion(
                    i,
                    onComplete: q =>
                    {
                        _questionQueue.Enqueue(q);
                        loaded++;
                    },
                    onError: err => Debug.LogWarning($"[GeminiQuizGenerator] Mock fallback error: {err}")
                );
            }

            Log($"Fallback loaded {loaded} questions from MockQuizGenerator.");
        }

        private void Log(string msg)
        {
            if (_logFlow)
                Debug.Log($"[GeminiQuizGenerator] {msg}");
        }

        // ── Status Getters (for UI / debugging) ──────────────────────────

        public bool IsReady => _isReady;
        public bool IsUsingFallback => _usingFallback;
        public int QueueCount => _questionQueue.Count;
    }
}