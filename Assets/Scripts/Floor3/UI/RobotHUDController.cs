// ============================================================
// FILE: Assets/Scripts/Floor3/UI/RobotHUDController.cs
// Namespace: Scripts.Floor3.UI
// ------------------------------------------------------------
// Displays robot HP and emotion state on screen.
// Subscribes to RobotEventBus — never touches RobotController.
//
// UI ELEMENTS:
//   _hpBar         → Image (Filled, Horizontal) — HP bar fill
//   _hpText        → TMPro "75 / 100"
//   _emotionIcon   → Image that swaps sprite per emotion
//   _warningPanel  → "⚠ ROBOT IN DANGER" shown when HP < 30%
//   _stunnedPanel  → "STUNNED" overlay shown during stun state
//
// MULTIPLAYER NOTE:
//   Runs on every client unchanged.
//   RobotEventBus fed by ClientRpc when Netcode arrives.
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.UI
{
    public class RobotHUDController : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("HP Bar")]
        [SerializeField] private Image           _hpBar;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private float           _maxHp = 100f;

        [Header("HP Bar Colors")]
        [SerializeField] private Color _hpColorHigh   = Color.green;
        [SerializeField] private Color _hpColorMedium = Color.yellow;
        [SerializeField] private Color _hpColorLow    = Color.red;

        [Header("Emotion Display")]
        [SerializeField] private Image  _emotionIcon;
        [SerializeField] private Sprite _emotionStable;
        [SerializeField] private Sprite _emotionConfused;
        [SerializeField] private Sprite _emotionPanicked;

        [Header("Warning Panels")]
        [SerializeField] private GameObject _warningPanel;   // Low HP warning
        [SerializeField] private GameObject _stunnedPanel;   // Stunned state overlay
        [SerializeField] private GameObject _escortComplete; // Level win overlay

        // ── Private State ─────────────────────────────────────────────────

        private float _currentNormalizedHp = 1f;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            RobotEventBus.OnRobotDamaged   += HandleRobotDamaged;
            RobotEventBus.OnStateChanged   += HandleStateChanged;
            RobotEventBus.OnEmotionChanged += HandleEmotionChanged;
            RobotEventBus.OnRobotDied      += HandleRobotDied;
            RobotEventBus.OnEscortComplete += HandleEscortComplete;
        }

        private void OnDisable()
        {
            RobotEventBus.OnRobotDamaged   -= HandleRobotDamaged;
            RobotEventBus.OnStateChanged   -= HandleStateChanged;
            RobotEventBus.OnEmotionChanged -= HandleEmotionChanged;
            RobotEventBus.OnRobotDied      -= HandleRobotDied;
            RobotEventBus.OnEscortComplete -= HandleEscortComplete;
        }

        private void Start()
        {
            // Initialize to full HP
            UpdateHpBar(1f);
            SetWarning(false);
            SetStunned(false);
            if (_escortComplete != null) _escortComplete.SetActive(false);
        }

        // ── Event Handlers ────────────────────────────────────────────────

        private void HandleRobotDamaged(float normalizedHp)
        {
            _currentNormalizedHp = normalizedHp;
            UpdateHpBar(normalizedHp);
            SetWarning(normalizedHp < 0.3f);
        }

        private void HandleStateChanged(RobotState newState)
        {
            SetStunned(newState == RobotState.Stunned);
        }

        private void HandleEmotionChanged(RobotEmotion newEmotion)
        {
            if (_emotionIcon == null) return;

            _emotionIcon.sprite = newEmotion switch
            {
                RobotEmotion.Stable   => _emotionStable,
                RobotEmotion.Confused => _emotionConfused,
                RobotEmotion.Panicked => _emotionPanicked,
                _                     => _emotionStable
            };
        }

        private void HandleRobotDied()
        {
            UpdateHpBar(0f);
            SetWarning(true);
            // DAY 5: trigger game over screen
        }

        private void HandleEscortComplete()
        {
            if (_escortComplete != null) _escortComplete.SetActive(true);
        }

        // ── HP Bar ────────────────────────────────────────────────────────

        private void UpdateHpBar(float normalized)
        {
            if (_hpBar != null)
            {
                _hpBar.fillAmount = normalized;
                _hpBar.color = normalized > 0.6f ? _hpColorHigh
                             : normalized > 0.3f ? _hpColorMedium
                             : _hpColorLow;
            }

            if (_hpText != null)
            {
                int current = Mathf.RoundToInt(normalized * _maxHp);
                _hpText.text = $"{current} / {(int)_maxHp}";
            }
        }

        // ── Panel Helpers ─────────────────────────────────────────────────

        private void SetWarning(bool active)
        {
            if (_warningPanel != null) _warningPanel.SetActive(active);
        }

        private void SetStunned(bool active)
        {
            if (_stunnedPanel != null) _stunnedPanel.SetActive(active);
        }
    }
}
