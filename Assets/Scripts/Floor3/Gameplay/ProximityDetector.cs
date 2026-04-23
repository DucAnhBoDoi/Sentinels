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
//   - Updates Robot Emotion (Confused/Stable) based on distance
// ============================================================

using UnityEngine;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class ProximityDetector : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Transform _robotTransform;
        [SerializeField] private Transform _playerA;
        [SerializeField] private Transform _playerB;
        [SerializeField] private DifficultyManager _difficultyManager;
        [SerializeField] private RobotController _robotController;

        // --- THÊM MỚI ---: Cần tham chiếu tới RobotStateMachine để đổi cảm xúc
        [SerializeField] private RobotStateMachine _robotStateMachine;
        // ----------------

        [Header("Distance Thresholds")]
        [Tooltip("Distance at which a single player triggers a warning.")]
        [SerializeField] private float _warnDistance = 5f;

        [Tooltip("Distance at which a player is considered 'too far' for difficulty.")]
        [SerializeField] private float _farDistance = 8f;

        [Tooltip("Robot only moves when BOTH players are within this distance.\n" +
                 "If ANY player leaves this zone, robot stops until they return.")]
        [SerializeField] private float _escortDistance = 6f;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        // ── Private State ─────────────────────────────────────────────────

        private bool _playerAFar = false;
        private bool _playerBFar = false;
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

            bool bothFar = _playerAFar && _playerBFar;

            // ── ESCORT GATE ───────────────────────────────────────────────
            bool playerAClose = distA <= _escortDistance;
            bool playerBClose = distB <= _escortDistance;

            // LUẬT MỚI: Bắt buộc CẢ 2 người phải ở trong vòng xanh lá cây (&&)
            bool bothPlayersClose = playerAClose && playerBClose;

            // NẾU CẢ 2 TỚI GẦN THÌ MỚI GỌI UIMANAGER BẬT BẢNG TOPIC
            if (bothPlayersClose)
            {
                if (Floor3UIManager.Instance != null)
                {
                    Floor3UIManager.Instance.ShowTopicSelection();
                }
            }

            // ROBOT CHỈ ĐI TIẾP NẾU CẢ 2 NGƯỜI CÙNG Ở TRONG VÒNG
            if (bothPlayersClose && !_escortGateOpen)
            {
                _escortGateOpen = true;
                _robotController?.SetEscortGate(true);
                Debug.Log("[ProximityDetector] Cả 2 Player đã vào vùng — robot tiếp tục chạy.");
            }
            // NẾU 1 TRONG 2 NGƯỜI RỜI VÒNG, ROBOT DỪNG LẠI CHỜ
            else if (!bothPlayersClose && _escortGateOpen)
            {
                _escortGateOpen = false;
                _robotController?.SetEscortGate(false);
                Debug.Log("[ProximityDetector] Có Player đi lạc — robot dừng lại chờ.");
            }

            // --- THÊM MỚI ---: CẬP NHẬT CẢM XÚC ROBOT ----------------------
            if (_robotStateMachine != null)
            {
                // Chỉ đổi sang Confused/Stable nếu robot KHÔNG đang hoảng loạn
                if (_robotStateMachine.CurrentEmotion != RobotEmotion.Panicked)
                {
                    if (bothFar)
                    {
                        // Cả hai người đều xa -> Bối rối
                        _robotStateMachine.ChangeEmotion(RobotEmotion.Confused);
                    }
                    else
                    {
                        // Ít nhất một người ở gần -> Bình tĩnh
                        _robotStateMachine.ChangeEmotion(RobotEmotion.Stable);
                    }
                }
            }
            // -------------------------------------------------------------

            // Feed DifficultyManager
            _difficultyManager?.UpdateProximity(bothFar);

            // Fire proximity events for UI
            ProximityEventBus.RaiseProximityUpdated(distA, distB, _warnDistance, _farDistance);
        }

        // ── Getters ───────────────────────────────────────────────────────

        public bool IsPlayerAFar() => _playerAFar;
        public bool IsPlayerBFar() => _playerBFar;
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