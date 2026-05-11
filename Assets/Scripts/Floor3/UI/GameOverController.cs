// ============================================================
// FILE: Assets/Scripts/Floor3/UI/GameOverController.cs
// Namespace: Scripts.Floor3.UI
// ────────────────────────────────────────────────────
// Listens to GameContext events and shows Win / Lose screens.
// Also displays final stats (time, wrong answers, accuracy).
//
// UI STRUCTURE (build in Inspector):
//   [UI_CANVAS]
//   ├── GameOverPanel      (root, hidden by default)
//   │   ├── TitleText      "MISSION FAILED" / "MISSION COMPLETE"
//   │   ├── ReasonText     "The robot was destroyed by viruses"
//   │   ├── StatsPanel
//   │   │   ├── TimeText       "Time: 2m 34s"
//   │   │   ├── AccuracyText   "Accuracy: 75%"
//   │   │   ├── WrongText      "Wrong answers: 2"
//   │   │   └── CheckpointText "Checkpoints: 5/5"
//   │   ├── RestartButton
//   │   └── MenuButton
//
// IMPORTANT: Uses Time.unscaledTime for show animation
//   because Time.timeScale may be 0 at game over.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.UI
{
    public class GameOverController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject _gameOverPanel;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _reasonText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _accuracyText;
        [SerializeField] private TextMeshProUGUI _wrongAnswersText;
        [SerializeField] private TextMeshProUGUI _checkpointText;

        [Header("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;

        [Header("Settings")]
        [Tooltip("Name of this scene — used for restart.")]
        [SerializeField] private string _sceneName = "GamePlayFloor3";

        [Tooltip("Name of the main menu scene.")]
        [SerializeField] private string _menuSceneName = "MainMenu";

        [Tooltip("Delay before showing the panel (seconds, unscaled).")]
        [SerializeField] private float _showDelay = 1.2f;

        [Header("Colors")]
        [SerializeField] private Color _winColor  = new Color(0.2f, 0.9f, 0.4f);
        [SerializeField] private Color _loseColor = new Color(0.9f, 0.2f, 0.2f);

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);

            _restartButton?.onClick.AddListener(OnRestartClicked);
            _menuButton?.onClick.AddListener(OnMenuClicked);
        }

        private void OnEnable()
        {
            GameContext.OnGameOver      += HandleGameOver;
            GameContext.OnLevelComplete += HandleLevelComplete;
        }

        private void OnDisable()
        {
            GameContext.OnGameOver      -= HandleGameOver;
            GameContext.OnLevelComplete -= HandleLevelComplete;
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void HandleGameOver(GameOverReason reason)
        {
            string reasonText = reason switch
            {
                GameOverReason.RobotDestroyed => "The robot was destroyed by viruses.",
                GameOverReason.TimeOut        => "Time has run out.",
                _                             => "Mission failed."
            };

            StartCoroutine(ShowPanel(
                title:      "MISSION FAILED",
                reason:     reasonText,
                titleColor: _loseColor
            ));
        }

        private void HandleLevelComplete()
        {
            StartCoroutine(ShowPanel(
                title:      "MISSION COMPLETE",
                reason:     "The Mechanical Soul has been escorted safely!",
                titleColor: _winColor
            ));
        }

        // ── Show Panel ────────────────────────────────────────────────────

        private IEnumerator ShowPanel(string title, string reason, Color titleColor)
        {
            // Restore timescale in case game is frozen (quiz was open at death)
            Time.timeScale = 1f;

            // Wait before showing (unscaled so it works regardless of timeScale)
            yield return new WaitForSecondsRealtime(_showDelay);

            // Populate text
            if (_titleText != null)
            {
                _titleText.text  = title;
                _titleText.color = titleColor;
            }

            if (_reasonText != null)
                _reasonText.text = reason;

            // Populate stats from GameContext
            var ctx = GameContext.Instance;
            if (ctx != null)
            {
                int    totalAnswers = ctx.WrongAnswerCount + ctx.CorrectAnswerCount;
                float  accuracy     = totalAnswers > 0
                    ? (float)ctx.CorrectAnswerCount / totalAnswers * 100f
                    : 100f;

                int minutes = Mathf.FloorToInt(ctx.TimeElapsed / 60f);
                int seconds = Mathf.FloorToInt(ctx.TimeElapsed % 60f);

                if (_timeText != null)
                    _timeText.text = $"Time: {minutes}m {seconds:00}s";

                if (_accuracyText != null)
                    _accuracyText.text = $"Accuracy: {accuracy:F0}%";

                if (_wrongAnswersText != null)
                    _wrongAnswersText.text = $"Wrong answers: {ctx.WrongAnswerCount}";

                if (_checkpointText != null)
                    _checkpointText.text = $"Checkpoints: {ctx.CheckpointsReached} / {ctx.TotalCheckpoints}";
            }

            _gameOverPanel?.SetActive(true);
        }

        // ── Button Handlers ───────────────────────────────────────────────

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_sceneName);
        }

        private void OnMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}
