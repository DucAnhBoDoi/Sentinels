// ============================================================
// FILE: Assets/Scripts/Floor3/Core/Floor3Brain.cs
// Namespace: Scripts.Floor3.Core
// ── UPDATED DAY 5 ──────────────────────────────────────────
// Changes:
//   - GameContext fully wired in (all DAY 5 hooks filled)
//   - OnCorrectAnswer / OnWrongAnswer update GameContext
//   - HandleRobotDied → GameContext.TriggerGameOver()
//   - HandleEscortComplete → GameContext.TriggerLevelComplete()
//   - HandleEmotionChanged → GameContext.UpdateEmotion()
//   - Start() → GameContext.StartTracking()
// ============================================================

using UnityEngine;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.Core
{
    public class Floor3Brain : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private RobotController    _robotController;
        [SerializeField] private QuizManager        _quizManager;
        [SerializeField] private VirusSpawner       _virusSpawner;
        [SerializeField] private DifficultyManager  _difficultyManager;

        // World position of the last checkpoint — used to localize virus spawns
        private Vector3 _lastCheckpointPosition;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            RobotEventBus.OnCheckpointReached += HandleCheckpointReached;
            RobotEventBus.OnStateChanged      += HandleStateChanged;
            RobotEventBus.OnEmotionChanged    += HandleEmotionChanged;
            RobotEventBus.OnRobotDamaged      += HandleRobotDamaged;
            RobotEventBus.OnRobotDied         += HandleRobotDied;
            RobotEventBus.OnEscortComplete    += HandleEscortComplete;
        }

        private void OnDisable()
        {
            RobotEventBus.OnCheckpointReached -= HandleCheckpointReached;
            RobotEventBus.OnStateChanged      -= HandleStateChanged;
            RobotEventBus.OnEmotionChanged    -= HandleEmotionChanged;
            RobotEventBus.OnRobotDamaged      -= HandleRobotDamaged;
            RobotEventBus.OnRobotDied         -= HandleRobotDied;
            RobotEventBus.OnEscortComplete    -= HandleEscortComplete;
        }

        private void Start()
        {
            if (_robotController == null) Debug.LogError("[Floor3Brain] RobotController missing!");
            if (_quizManager     == null) Debug.LogError("[Floor3Brain] QuizManager missing!");
            if (_virusSpawner    == null) Debug.LogWarning("[Floor3Brain] VirusSpawner not assigned.");

            // Start game time tracking
            GameContext.Instance?.StartTracking();
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void HandleCheckpointReached(int waypointIndex)
        {
            _lastCheckpointPosition = _robotController.transform.position;
            GameContext.Instance?.RegisterCheckpoint();
            Debug.Log($"[Floor3Brain] Checkpoint {waypointIndex} → quiz start.");
            _quizManager.StartQuiz(waypointIndex);
        }

        private void HandleStateChanged(RobotState newState)
        {
            Debug.Log($"[Floor3Brain] Robot state → {newState}");
        }

        private void HandleEmotionChanged(RobotEmotion newEmotion)
        {
            GameContext.Instance?.UpdateEmotion(newEmotion);
            Debug.Log($"[Floor3Brain] Robot emotion → {newEmotion}");
        }

        private void HandleRobotDamaged(float normalizedHp)
        {
            GameContext.Instance?.UpdateRobotHp(normalizedHp);
        }

        private void HandleRobotDied()
        {
            Debug.Log("[Floor3Brain] Robot died → Game Over.");
            _virusSpawner?.ClearAllViruses();
            GameContext.Instance?.TriggerGameOver(GameOverReason.RobotDestroyed);
        }

        private void HandleEscortComplete()
        {
            Debug.Log("[Floor3Brain] Escort complete → Level Won!");
            _virusSpawner?.ClearAllViruses();
            GameContext.Instance?.TriggerLevelComplete();
        }

        // ── Public API (called by QuizManager) ───────────────────────────

        public void OnCorrectAnswer()
        {
            Debug.Log("[Floor3Brain] Correct → speed boost.");
            GameContext.Instance?.RegisterCorrectAnswer();
            _robotController.ApplySpeedBoost();
        }

        public void OnWrongAnswer()
        {
            Debug.Log($"[Floor3Brain] Wrong → stun + viruses.");
            GameContext.Instance?.RegisterWrongAnswer();
            _robotController.ApplyStun();
            _virusSpawner?.SpawnWave(_lastCheckpointPosition);
            _difficultyManager?.RegisterWrongAnswer();
        }
    }
}
