// ============================================================
// FILE: Assets/Scripts/Floor3/UI/QuizHUDController.cs
// Namespace: Scripts.Floor3.UI
// ------------------------------------------------------------
// Owns all Quiz UI. Subscribes to QuizEventBus.
// NEVER references QuizManager directly.
//
// UI ELEMENTS (assign in Inspector):
//   _quizPanel         → root panel GameObject (show/hide)
//   _questionText      → TMPro question text
//   _answerButtons[]   → 4 answer buttons (UI Buttons)
//   _answerTexts[]     → TMPro text on each button
//   _timerBar          → Image with fillAmount (0–1)
//   _timerText         → "12.3s" countdown text
//   _playerAIndicator  → highlight showing A's selection
//   _playerBIndicator  → highlight showing B's selection
//   _conflictWarning   → "⚠ PLAYERS DISAGREE" panel
//   _resultFeedback    → "✓ CORRECT!" / "✗ WRONG!" text
//
// MULTIPLAYER NOTE:
//   This script runs on EVERY client.
//   It reads from QuizEventBus which will be fed by ClientRpc.
//   No changes needed here when multiplayer arrives.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Floor3.Core;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.UI
{
    public class QuizHUDController : MonoBehaviour
    {
        // ── Inspector References ──────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject _quizPanel;
        [SerializeField] private GameObject _conflictWarning;

        [Header("Question")]
        [SerializeField] private TextMeshProUGUI _questionText;

        [Header("Answer Buttons (exactly 4)")]
        [SerializeField] private Button[]          _answerButtons  = new Button[4];
        [SerializeField] private TextMeshProUGUI[] _answerTexts    = new TextMeshProUGUI[4];

        [Header("Player Selection Highlights")]
        [Tooltip("One Image per answer slot, tinted for Player A selection")]
        [SerializeField] private Image[] _playerAHighlights = new Image[4];
        [Tooltip("One Image per answer slot, tinted for Player B selection")]
        [SerializeField] private Image[] _playerBHighlights = new Image[4];

        [Header("Timer")]
        [SerializeField] private Image            _timerBar;
        [SerializeField] private TextMeshProUGUI  _timerText;

        [Header("Result Feedback")]
        [SerializeField] private TextMeshProUGUI _resultFeedback;
        [SerializeField] private float           _resultDisplayDuration = 1.5f;

        [Header("Colors")]
        [SerializeField] private Color _playerAColor    = new Color(0.2f, 0.6f, 1f, 0.6f);
        [SerializeField] private Color _playerBColor    = new Color(1f, 0.4f, 0.2f, 0.6f);
        [SerializeField] private Color _correctColor    = Color.green;
        [SerializeField] private Color _wrongColor      = Color.red;
        [SerializeField] private Color _defaultBtnColor = Color.white;

        // ── Private State ─────────────────────────────────────────────────

        private QuizQuestion _currentQuestion;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            SetPanelActive(false);
            SetConflictActive(false);
            ClearHighlights();
            SetResultText("", Color.white);

            // Wire up answer button clicks → QuizEventBus
            // Buttons let players click as an ALTERNATIVE to keyboard
            for (int i = 0; i < _answerButtons.Length; i++)
            {
                int captured = i; // closure capture
                if (_answerButtons[i] != null)
                {
                    _answerButtons[i].onClick.RemoveAllListeners();
                    _answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(captured));
                }
            }
        }

        private void OnEnable()
        {
            QuizEventBus.OnQuizStarted      += HandleQuizStarted;
            QuizEventBus.OnTimerTick        += HandleTimerTick;
            QuizEventBus.OnTimerExpired     += HandleTimerExpired;
            QuizEventBus.OnPlayerConfirmed  += HandlePlayerConfirmed;
            QuizEventBus.OnConflictDetected += HandleConflictDetected;
            QuizEventBus.OnQuizResolved     += HandleQuizResolved;
        }

        private void OnDisable()
        {
            QuizEventBus.OnQuizStarted      -= HandleQuizStarted;
            QuizEventBus.OnTimerTick        -= HandleTimerTick;
            QuizEventBus.OnTimerExpired     -= HandleTimerExpired;
            QuizEventBus.OnPlayerConfirmed  -= HandlePlayerConfirmed;
            QuizEventBus.OnConflictDetected -= HandleConflictDetected;
            QuizEventBus.OnQuizResolved     -= HandleQuizResolved;
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void HandleQuizStarted(QuizQuestion question)
        {
            _currentQuestion = question;

            SetPanelActive(true);
            SetConflictActive(false);
            ClearHighlights();
            SetResultText("", Color.white);

            // Populate question text
            if (_questionText != null)
                _questionText.text = question.QuestionText;

            // Populate answer buttons
            for (int i = 0; i < _answerTexts.Length; i++)
            {
                if (_answerTexts[i] != null)
                    _answerTexts[i].text = (i < question.Answers.Length)
                        ? question.Answers[i]
                        : "";
            }

            // Reset timer bar
            if (_timerBar != null) _timerBar.fillAmount = 1f;
        }

        private void HandleTimerTick(float normalized, float secondsLeft)
        {
            if (_timerBar  != null) _timerBar.fillAmount = normalized;
            if (_timerText != null) _timerText.text = $"{secondsLeft:F1}s";

            // Pulse red when under 5 seconds
            if (_timerBar != null)
                _timerBar.color = secondsLeft <= 5f
                    ? Color.Lerp(Color.red, Color.yellow, secondsLeft / 5f)
                    : Color.green;
        }

        private void HandleTimerExpired()
        {
            if (_timerBar  != null) _timerBar.fillAmount = 0f;
            if (_timerText != null) _timerText.text = "0.0s";
        }

        private void HandlePlayerConfirmed(PlayerSlot slot, int answerIndex)
        {
            SetConflictActive(false);

            Image[] highlights = (slot == PlayerSlot.PlayerA)
                ? _playerAHighlights
                : _playerBHighlights;

            Color color = (slot == PlayerSlot.PlayerA) ? _playerAColor : _playerBColor;

            // Clear previous highlight for this player, set new one
            for (int i = 0; i < highlights.Length; i++)
            {
                if (highlights[i] == null) continue;
                highlights[i].color  = (i == answerIndex) ? color : new Color(0, 0, 0, 0);
                highlights[i].gameObject.SetActive(true);
            }
        }

        private void HandleConflictDetected(int answerA, int answerB)
        {
            SetConflictActive(true);
            ClearHighlights(); // Reset — players must re-pick
        }

        private void HandleQuizResolved(bool isCorrect, int correctIndex)
        {
            SetConflictActive(false);

            // Flash correct answer green
            if (correctIndex >= 0 && correctIndex < _answerButtons.Length)
            {
                var btnColors = _answerButtons[correctIndex].colors;
                btnColors.normalColor = _correctColor;
                _answerButtons[correctIndex].colors = btnColors;
            }

            string msg   = isCorrect ? "CORRECT!" : "WRONG!";
            Color  color = isCorrect ? _correctColor : _wrongColor;
            SetResultText(msg, color);

            StartCoroutine(HidePanelAfterDelay(_resultDisplayDuration));
        }

        private IEnumerator HidePanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetPanelActive(false);
            ClearHighlights();
        }

        // ── Button Click ─────────────────────────────────────────────────
        // Buttons fire for BOTH players — whoever clicks is PlayerA by default.
        // For split-screen / keyboard-only, use QuizInputHandler keys instead.
        // Expand this logic on Day 5 if mouse-click roles are needed.

        private void OnAnswerButtonClicked(int index)
        {
            // In keyboard-only setup, button clicks are supplementary.
            // For now, clicking fires PlayerA answer (can be expanded Day 5).
            QuizEventBus.RaisePlayerAAnswered(index);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void SetPanelActive(bool active)
        {
            if (_quizPanel != null) _quizPanel.SetActive(active);
        }

        private void SetConflictActive(bool active)
        {
            if (_conflictWarning != null) _conflictWarning.SetActive(active);
        }

        private void ClearHighlights()
        {
            foreach (var img in _playerAHighlights)
                if (img != null) img.color = new Color(0, 0, 0, 0);
            foreach (var img in _playerBHighlights)
                if (img != null) img.color = new Color(0, 0, 0, 0);

            // Reset button colors
            foreach (var btn in _answerButtons)
            {
                if (btn == null) continue;
                var colors = btn.colors;
                colors.normalColor = _defaultBtnColor;
                btn.colors = colors;
            }
        }

        private void SetResultText(string msg, Color color)
        {
            if (_resultFeedback == null) return;
            _resultFeedback.text  = msg;
            _resultFeedback.color = color;
        }
    }
}
