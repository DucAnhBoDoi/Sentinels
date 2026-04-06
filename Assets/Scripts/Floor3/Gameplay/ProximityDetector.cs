// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/ProximityDetector.cs
// Namespace: Scripts.Floor3.Gameplay
// ── DAY 4 ──────────────────────────────────────────────────
// Checks distance of Player A and Player B to the robot.
//
// ESCORT GATE (new feature):
//   Robot only moves when AT LEAST ONE player is within
//   _escortDistance. If both players leave the escort zone,
//   robot stops (SetEscortGate false). Resumes when a player
//   returns close enough.
//
// ALSO:
//   - Fires UI warning when either player is too far
//   - Feeds DifficultyManager with proximity data
//   - ProximityEventBus events for HUD
// ============================================================

using UnityEngine;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class ProximityDetector : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Transform       _robotTransform;
        [SerializeField] private Transform       _playerA;
        [SerializeField] private Transform       _playerB;
        [SerializeField] private DifficultyManager _difficultyManager;
        [SerializeField] private RobotController _robotController;

        [Header("Distance Thresholds")]
        [Tooltip("Distance at which a single player triggers a warning.")]
        [SerializeField] private float _warnDistance   = 5f;

        [Tooltip("Distance at which a player is considered 'too far' for difficulty.")]
        [SerializeField] private float _farDistance    = 8f;

        [Tooltip("Robot only moves when at least 1 player is within this distance.\n" +
                 "If BOTH players leave this zone, robot stops until one returns.")]
        [SerializeField] private float _escortDistance = 6f;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos      = true;

        // ── Private State ─────────────────────────────────────────────────

        private bool _playerAFar    = false;
        private bool _playerBFar    = false;
        private bool _escortGateOpen = true; // true = robot allowed to move

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Update()
        {
            if (_robotTransform == null) return;

            float distA = _playerA != null
                ? Vector2.Distance(_playerA.position, _robotTransform.position)
                : 0f;
            float distB = _playerB != null
                ? Vector2.Distance(_playerB.position, _robotTransform.position)
                : 0f;

            _playerAFar = distA > _farDistance;
            _playerBFar = distB > _farDistance;

            bool bothFar        = _playerAFar && _playerBFar;

            // ── ESCORT GATE ───────────────────────────────────────────────
            // Robot moves only when at least 1 player is within _escortDistance.
            // Uses the CLOSER player — robot waits for either player, not both.
            bool playerAClose = distA <= _escortDistance;
            bool playerBClose = distB <= _escortDistance;
            bool anyPlayerClose = playerAClose || playerBClose;

            if (anyPlayerClose && !_escortGateOpen)
            {
                _escortGateOpen = true;
                _robotController?.SetEscortGate(true);
                Debug.Log("[ProximityDetector] Player returned — robot may move.");
            }
            else if (!anyPlayerClose && _escortGateOpen)
            {
                _escortGateOpen = false;
                _robotController?.SetEscortGate(false);
                Debug.Log("[ProximityDetector] Both players too far — robot waiting.");
            }

            // Feed DifficultyManager
            _difficultyManager?.UpdateProximity(bothFar);

            // Fire proximity events for UI
            ProximityEventBus.RaiseProximityUpdated(distA, distB, _warnDistance, _farDistance);
        }

        // ── Getters ───────────────────────────────────────────────────────

        public bool IsPlayerAFar()    => _playerAFar;
        public bool IsPlayerBFar()    => _playerBFar;
        public bool IsEscortGateOpen() => _escortGateOpen;

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!_drawGizmos || _robotTransform == null) return;

            // Escort gate ring — green (robot moves inside this)
            Gizmos.color = _escortGateOpen
                ? new Color(0f, 1f, 0f, 0.3f)
                : new Color(1f, 0.5f, 0f, 0.5f); // orange when gate closed
            Gizmos.DrawWireSphere(_robotTransform.position, _escortDistance);

            // Warning ring — yellow
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(_robotTransform.position, _warnDistance);

            // Far ring — red
            Gizmos.color = new Color(1f, 0f, 0f, 0.12f);
            Gizmos.DrawWireSphere(_robotTransform.position, _farDistance);

            // Lines to players
            if (_playerA != null)
            {
                Gizmos.color = _playerAFar ? Color.red : Color.green;
                Gizmos.DrawLine(_robotTransform.position, _playerA.position);
            }
            if (_playerB != null)
            {
                Gizmos.color = _playerBFar ? Color.red : Color.green;
                Gizmos.DrawLine(_robotTransform.position, _playerB.position);
            }
        }
    }
}