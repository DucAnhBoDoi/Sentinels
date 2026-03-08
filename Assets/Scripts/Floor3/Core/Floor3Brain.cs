// ============================================================
// FILE: Assets/Scripts/Floor3/Core/Floor3Brain.cs
// Namespace: Scripts.Floor3.Core
// ── UPDATED DAY 2 ──────────────────────────────────────────
// Changes from Day 1:
//   - QuizManager reference wired in (was commented out)
//   - HandleCheckpointReached now calls _quizManager.StartQuiz()
//   - Debug auto-resume REMOVED (QuizManager handles resume flow)
//   - OnCorrectAnswer / OnWrongAnswer now fully connected
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

        // DifficultyManager added Day 4
        // [SerializeField] private DifficultyManager _difficultyManager;

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
                Debug.LogError("[Floor3Brain] RobotController reference missing!");
            if (_quizManager == null)
                Debug.LogError("[Floor3Brain] QuizManager reference missing!");
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void HandleCheckpointReached(int waypointIndex)
        {
            Debug.Log($"[Floor3Brain] Checkpoint at waypoint {waypointIndex} → Starting quiz.");
            _quizManager.StartQuiz(waypointIndex);
            // Robot stays in Waiting state until QuizManager calls back via
            // OnCorrectAnswer() or OnWrongAnswer() below
        }

        private void HandleStateChanged(RobotState newState)
        {
            // DAY 4: _difficultyManager.OnRobotStateChanged(newState);
            Debug.Log($"[Floor3Brain] Robot state → {newState}");
        }

        private void HandleEmotionChanged(RobotEmotion newEmotion)
        {
            // DAY 5: GameContext.Instance.UpdateEmotion(newEmotion);
            // DAY 6: LLM prompt context updated here
            Debug.Log($"[Floor3Brain] Robot emotion → {newEmotion}");
        }

        private void HandleRobotDied()
        {
            Debug.Log("[Floor3Brain] Robot died → Game Over.");
            // DAY 5: GameContext.Instance.TriggerGameOver();
        }

        private void HandleEscortComplete()
        {
            Debug.Log("[Floor3Brain] Escort complete → Level Won!");
            // DAY 5: GameContext.Instance.TriggerLevelComplete();
        }

        // ── Public API (called by QuizManager) ───────────────────────────

        /// <summary>Quiz resolved with correct answer. Robot gets speed boost.</summary>
        public void OnCorrectAnswer()
        {
            Debug.Log("[Floor3Brain] Correct answer → speed boost + resume.");
            _robotController.ApplySpeedBoost();
            // Note: ApplySpeedBoost() internally calls ChangeState(Accelerated)
            // which transitions to Moving. Robot resumes automatically.
        }

        /// <summary>Quiz resolved with wrong answer or timeout. Robot stunned, viruses spawn.</summary>
        public void OnWrongAnswer()
        {
            Debug.Log("[Floor3Brain] Wrong answer → stun + virus spawn.");
            _robotController.ApplyStun();
            // DAY 3: _virusSpawner.SpawnWave();
            // DAY 4: _difficultyManager.RegisterWrongAnswer();
        }
    }
}
