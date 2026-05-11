// ============================================================
// FILE: Assets/Scripts/Floor3/AI/GeminiAPIService.cs
// Namespace: Scripts.Floor3.AI
// ───────────────────────────────────────────────────
// Responsible for ONE thing: HTTP POST to the Vercel backend.
//
// FLOW:
//   1. Send POST /api/quiz with { "topic": "technology" }
//   2. Wait max 15 seconds (Unity fallback timeout)
//   3. If response OK → parse JSON → return List<QuizQuestion>
//   4. If timeout or error → return null (caller uses Mock)
//
// WHY 15s TIMEOUT IN UNITY (backend already has 8s)?
//   Backend timeout = protects Vercel from hanging.
//   Unity timeout   = protects the PLAYER from waiting too long.
//   If Unity fallback fires at 15s, the game starts immediately
//   with MockQuizGenerator questions. No player ever waits.
//
// ARCHITECTURE NOTE:
//   This class is PURE HTTP — no quiz logic, no UI, no game state.
//   GeminiQuizGenerator uses this service and handles game logic.
//
// MULTIPLAYER NOTE:
//   Only Host calls FetchQuizzes().
//   Wrap the call with: if (!IsHost) return;
// ============================================================

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.AI
{
    public class GeminiAPIService : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Backend Configuration")]
        [Tooltip("Your Vercel backend URL. Example:\nhttps://floor3-quiz-backend.vercel.app/api/quiz")]
        [SerializeField] private string _backendUrl = "https://your-backend.vercel.app/api/quiz";

        [Tooltip("Seconds before Unity gives up and uses fallback.")]
        [SerializeField] private float _timeoutSeconds = 15f;

        [Header("Debug")]
        [SerializeField] private bool _logRequests = true;

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Fetch 5 quiz questions from the Vercel backend for the given topic.
        /// onSuccess: called with parsed questions (count may be 1–5)
        /// onFailure: called if request fails or times out — caller uses Mock
        /// </summary>
        public void FetchQuizzes(
            string topic,
            Action<List<QuizQuestion>> onSuccess,
            Action<string>             onFailure)
        {
            StartCoroutine(FetchCoroutine(topic, onSuccess, onFailure));
        }

        // ── HTTP Coroutine ────────────────────────────────────────────────

        private IEnumerator FetchCoroutine(
            string topic,
            Action<List<QuizQuestion>> onSuccess,
            Action<string>             onFailure)
        {
            Log($"Sending request → topic: {topic}");

            // Build request body
            string bodyJson = $"{{\"topic\":\"{topic}\"}}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

            using var request = new UnityWebRequest(_backendUrl, "POST");
            request.uploadHandler   = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.CeilToInt(_timeoutSeconds); // seconds, integer

            // Send request
            var operation = request.SendWebRequest();

            // Wait with manual timeout check
            float elapsed = 0f;
            while (!operation.isDone)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= _timeoutSeconds)
                {
                    request.Abort();
                    string msg = $"Request timed out after {_timeoutSeconds}s";
                    Log($"⚠ {msg} → using fallback.");
                    onFailure?.Invoke(msg);
                    yield break;
                }
                yield return null;
            }

            // Handle network errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                string msg = $"Network error: {request.error}";
                Log($"⚠ {msg} → using fallback.");
                onFailure?.Invoke(msg);
                yield break;
            }

            // Parse response
            string responseJson = request.downloadHandler.text;
            Log($"Response received ({responseJson.Length} chars). Parsing...");

            var questions = QuizDataParser.Parse(responseJson);

            if (questions == null || questions.Count == 0)
            {
                string msg = "Parser returned null — invalid JSON structure.";
                Log($"⚠ {msg} → using fallback.");
                onFailure?.Invoke(msg);
                yield break;
            }

            Log($"✓ Success — {questions.Count} questions loaded from backend.");
            onSuccess?.Invoke(questions);
        }

        private void Log(string msg)
        {
            if (_logRequests) Debug.Log($"[GeminiAPIService] {msg}");
        }
    }
}
