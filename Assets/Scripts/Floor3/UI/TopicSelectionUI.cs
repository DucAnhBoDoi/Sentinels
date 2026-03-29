// ============================================================
// FILE: Assets/Scripts/Floor3/UI/TopicSelectionUI.cs
// Namespace: Scripts.Floor3.UI
// ── STEP 2 ─────────────────────────────────────────────────
// Shown at the START of the level, before the robot moves.
// Players choose one of 3 topics → backend fetches 5 questions
// → quiz panel hides → game begins.
//
// FLOW:
//   Level loads → TopicSelectionUI shows (game paused)
//   Players press topic button → FetchAndPreload() called
//   Loading spinner shown → backend responds (≤3s)
//   Questions ready → panel hides → robot starts moving
//
// UI STRUCTURE:
//   [UI_CANVAS]
//   └── TopicSelectionPanel
//       ├── TitleText          "Choose Your Quiz Topic"
//       ├── SubtitleText       "Both players must agree"
//       ├── ButtonPanel
//       │   ├── TechnologyButton   "⚙ Technology"
//       │   ├── BiologyButton      "🧬 Biology"
//       │   └── EthicsButton       "⚖ Ethics"
//       ├── LoadingPanel       (hidden until topic chosen)
//       │   ├── LoadingText    "Generating questions..."
//       │   └── SourceText     "" → "✓ AI Generated" / "⚠ Using Backup"
//       └── StartButton        (hidden until ready)
//
// MULTIPLAYER NOTE:
//   Only HOST can click buttons.
//   Selected topic + questions broadcast via ClientRpc.
//   Clients show loading panel passively until Host confirms.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Floor3.AI;

namespace Scripts.Floor3.UI
{
    public class TopicSelectionUI : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject _selectionPanel;
        [SerializeField] private GameObject _loadingPanel;

        [Header("Topic Buttons")]
        [SerializeField] private Button _technologyButton;
        [SerializeField] private Button _biologyButton;
        [SerializeField] private Button _ethicsButton;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _sourceText;

        [Header("Start Button")]
        [Tooltip("Shown after questions are ready. Players confirm to begin.")]
        [SerializeField] private Button             _startButton;
        [SerializeField] private TextMeshProUGUI    _startButtonText;

        [Header("References")]
        [SerializeField] private GeminiQuizGenerator _geminiGenerator;

        [Header("Debug")]
        [SerializeField] private bool _logFlow = true;

        // ── Private State ─────────────────────────────────────────────────

        private string _selectedTopic  = "";
        private bool   _questionsReady = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            // Pause game while topic selection is open
            Time.timeScale = 0f;

            // Wire buttons
            _technologyButton?.onClick.AddListener(() => OnTopicSelected("technology", "⚙ Technology"));
            _biologyButton?.onClick.AddListener(   () => OnTopicSelected("biology",    "🧬 Biology"));
            _ethicsButton?.onClick.AddListener(    () => OnTopicSelected("ethics",     "⚖ Ethics"));
            _startButton?.onClick.AddListener(OnStartClicked);

            // Initial state
            ShowSelectionPanel();
            SetLoadingPanel(false);
            SetStartButton(false);
        }

        // ── Topic Selection ───────────────────────────────────────────────

        private void OnTopicSelected(string topic, string label)
        {
            if (_questionsReady) return; // already loading

            _selectedTopic  = topic;
            _questionsReady = false;

            Log($"Topic selected: {label}");

            // Hide topic buttons, show loading
            SetButtonsInteractable(false);
            SetLoadingPanel(true);
            SetLoadingText("Generating questions with AI...");
            SetSourceText("");
            SetStartButton(false);

            // Fetch questions — uses unscaled time since timeScale = 0
            _geminiGenerator.FetchAndPreload(topic, OnQuestionsReady);
        }

        private void OnQuestionsReady()
        {
            _questionsReady = true;

            bool usingFallback = _geminiGenerator.IsUsingFallback;

            SetLoadingText("Questions ready!");
            SetSourceText(usingFallback
                ? "⚠ Using backup questions (AI unavailable)"
                : "✓ AI-generated questions loaded");

            Log($"Questions ready. Fallback: {usingFallback}. Count: {_geminiGenerator.QueueCount}");

            // Show start button
            SetStartButton(true);
            if (_startButtonText != null)
                _startButtonText.text = "▶ Start Mission";
        }

        // ── Start Game ────────────────────────────────────────────────────

        private void OnStartClicked()
        {
            if (!_questionsReady) return;

            Log("Start clicked → hiding panel, resuming game.");

            // Resume time
            Time.timeScale = 1f;

            // Hide entire selection UI
            if (_selectionPanel != null)
                _selectionPanel.SetActive(false);
        }

        // ── UI Helpers ────────────────────────────────────────────────────

        private void ShowSelectionPanel()
        {
            if (_selectionPanel != null) _selectionPanel.SetActive(true);
        }

        private void SetLoadingPanel(bool active)
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(active);
        }

        private void SetLoadingText(string text)
        {
            if (_loadingText != null) _loadingText.text = text;
        }

        private void SetSourceText(string text)
        {
            if (_sourceText != null) _sourceText.text = text;
        }

        private void SetStartButton(bool active)
        {
            if (_startButton != null) _startButton.gameObject.SetActive(active);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_technologyButton != null) _technologyButton.interactable = interactable;
            if (_biologyButton    != null) _biologyButton.interactable    = interactable;
            if (_ethicsButton     != null) _ethicsButton.interactable     = interactable;
        }

        private void Log(string msg)
        {
            if (_logFlow) Debug.Log($"[TopicSelectionUI] {msg}");
        }
    }
}
