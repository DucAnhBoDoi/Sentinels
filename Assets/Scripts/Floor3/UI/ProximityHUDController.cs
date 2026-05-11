// ============================================================
// FILE: Assets/Scripts/Floor3/UI/ProximityHUDController.cs
// Namespace: Scripts.Floor3.UI
// ────────────────────────────────────────────────────
// Shows visual warnings when players are too far from robot.
//
// UI STRUCTURE:
//   [UI_CANVAS]
//   └── ProximityHUD
//       ├── PlayerA_Warning   "⚠ PLAYER A TOO FAR!" (red text/panel)
//       ├── PlayerB_Warning   "⚠ PLAYER B TOO FAR!"
//       └── BothFar_Warning   "⚠ ROBOT UNESCORTED — RETURN NOW!"
//
// Subscribes to ProximityEventBus (never touches ProximityDetector).
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.UI
{
    public class ProximityHUDController : MonoBehaviour
    {
        [Header("Warning Panels")]
        [SerializeField] private GameObject _playerAWarning;
        [SerializeField] private GameObject _playerBWarning;
        [SerializeField] private GameObject _bothFarWarning;

        [Header("Distance Bars (optional)")]
        [Tooltip("Fill image showing Player A distance to robot (0=close, 1=far)")]
        [SerializeField] private Image _playerADistanceBar;
        [Tooltip("Fill image showing Player B distance to robot")]
        [SerializeField] private Image _playerBDistanceBar;

        [Header("Colors")]
        [SerializeField] private Color _safeColor = Color.green;
        [SerializeField] private Color _warnColor = Color.yellow;
        [SerializeField] private Color _farColor  = Color.red;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            SetActive(_playerAWarning, false);
            SetActive(_playerBWarning, false);
            SetActive(_bothFarWarning, false);
        }

        private void OnEnable()
        {
            ProximityEventBus.OnProximityUpdated += HandleProximityUpdated;
        }

        private void OnDisable()
        {
            ProximityEventBus.OnProximityUpdated -= HandleProximityUpdated;
        }

        // ── Event Handler ─────────────────────────────────────────────────

        private void HandleProximityUpdated(
            float distA, float distB, float warnThreshold, float farThreshold)
        {
            bool aWarn = distA > warnThreshold;
            bool bWarn = distB > warnThreshold;
            bool aFar  = distA > farThreshold;
            bool bFar  = distB > farThreshold;

            SetActive(_playerAWarning, aFar);
            SetActive(_playerBWarning, bFar);
            SetActive(_bothFarWarning, aFar && bFar);

            // Update distance bars
            if (_playerADistanceBar != null)
            {
                float t = Mathf.Clamp01(distA / farThreshold);
                _playerADistanceBar.fillAmount = t;
                _playerADistanceBar.color = aFar ? _farColor : aWarn ? _warnColor : _safeColor;
            }

            if (_playerBDistanceBar != null)
            {
                float t = Mathf.Clamp01(distB / farThreshold);
                _playerBDistanceBar.fillAmount = t;
                _playerBDistanceBar.color = bFar ? _farColor : bWarn ? _warnColor : _safeColor;
            }
        }

        private void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
