// ============================================================
// FILE: Assets/Scripts/Floor3/Core/Floor3Brain.cs
// Namespace: Scripts.Floor3.Core
// ── UPDATED DAY 3 ──────────────────────────────────────────
// Changes:
//   - Stores _lastCheckpointPosition when checkpoint fires
//   - OnWrongAnswer() passes that position to SpawnWave()
//     so viruses spawn NEAR the failed checkpoint, not randomly
//   - VirusSpawner.ClearAllViruses() on robot death / escort done
// ============================================================

using UnityEngine;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.Core
{
    public class Floor3Brain : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private RobotController _robotController;
        [SerializeField] private QuizManager     _quizManager;
        [SerializeField] private VirusSpawner    _virusSpawner;
        // [SerializeField] private DifficultyManager _difficultyManager; // Day 4

        // World position of the last checkpoint waypoint reached.
        // Used to localize wrong-answer virus spawns.
        private Vector3 _lastCheckpointPosition;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void OnEnable()
        {
            RobotEventBus.OnCheckpointReached += HandleCheckpointReached;
            RobotEventBus.OnStateChanged      += HandleStateChanged;
            RobotEventBus.OnEmotionChanged    += HandleEmotionChanged;
            RobotEventBus.OnRobotDied         += HandleRobotDied;
            RobotEventBus.OnEscortComplete    += HandleEscortComplete;
        }

        private void OnDisable()
        {
            RobotEventBus.OnCheckpointReached -= HandleCheckpointReached;
            RobotEventBus.OnStateChanged      -= HandleStateChanged;
            RobotEventBus.OnEmotionChanged    -= HandleEmotionChanged;
            RobotEventBus.OnRobotDied         -= HandleRobotDied;
            RobotEventBus.OnEscortComplete    -= HandleEscortComplete;
        }

        private void Start()
        {
            if (_robotController == null)
                Debug.LogError("[Floor3Brain] RobotController missing!");
            if (_quizManager == null)
                Debug.LogError("[Floor3Brain] QuizManager missing!");
            if (_virusSpawner == null)
                Debug.LogWarning("[Floor3Brain] VirusSpawner not assigned — no virus spawning.");
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void HandleCheckpointReached(int waypointIndex)
        {
            // Store the robot's current position as the checkpoint position.
            // Robot snaps to waypoint on arrival so this is accurate.
            _lastCheckpointPosition = _robotController.transform.position;

            Debug.Log($"[Floor3Brain] Checkpoint {waypointIndex} at {_lastCheckpointPosition} → quiz start.");
            _quizManager.StartQuiz(waypointIndex);
        }

        private void HandleStateChanged(RobotState newState)
        {
            // DAY 4: _difficultyManager.OnRobotStateChanged(newState);
            Debug.Log($"[Floor3Brain] Robot state → {newState}");
        }

        private void HandleEmotionChanged(RobotEmotion newEmotion)
        {
            // DAY 5: GameContext.Instance.UpdateEmotion(newEmotion);
            Debug.Log($"[Floor3Brain] Robot emotion → {newEmotion}");
        }

        private void HandleRobotDied()
        {
            Debug.Log("[Floor3Brain] Robot died → clearing viruses.");
            _virusSpawner?.ClearAllViruses();
            // DAY 5: GameContext.Instance.TriggerGameOver();
        }

        private void HandleEscortComplete()
        {
            Debug.Log("[Floor3Brain] Escort complete → clearing viruses.");
            _virusSpawner?.ClearAllViruses();
            // DAY 5: GameContext.Instance.TriggerLevelComplete();
        }

        // ── Public API (called by QuizManager) ───────────────────────────

        public void OnCorrectAnswer()
        {
            Debug.Log("[Floor3Brain] Correct → speed boost.");
            _robotController.ApplySpeedBoost();
        }

        public void OnWrongAnswer()
        {
            Debug.Log($"[Floor3Brain] Wrong → stun + spawn near {_lastCheckpointPosition}.");
            _robotController.ApplyStun();
            // Spawn near the checkpoint where the wrong answer happened
            _virusSpawner?.SpawnWave(_lastCheckpointPosition);
            // DAY 4: _difficultyManager.RegisterWrongAnswer();
        }
    }
}