// ============================================================
// FILE: Assets/Scripts/Floor3/Core/DifficultyManager.cs
// Namespace: Scripts.Floor3.Core
// ── DAY 4 ──────────────────────────────────────────────────
// Central authority for all difficulty decisions in Floor3.
//
// WHAT IT TRACKS:
//   - Wrong answer count
//   - Player proximity violations (both players too far)
//   - Time elapsed
//
// WHAT IT CONTROLS (via public API, never direct field access):
//   - VirusSpawner: continuous interval + wave count
//   - QuizManager: time limit per question
//   - RobotStateMachine: emotion (Confused when far from robot)
//
// DESIGN RULES:
//   - NOTHING else decides difficulty — all scaling flows through here
//   - Robot has NO difficulty logic
//   - VirusSpawner has NO difficulty logic
//   - QuizManager has NO difficulty logic
//   They just expose setters that DifficultyManager calls.
//
// DIFFICULTY TIERS:
//   Easy   (0–1 wrong, players close)    → slow spawn, long timer
//   Medium (2–3 wrong OR players far)    → faster spawn, shorter timer
//   Hard   (4+ wrong OR very far long)   → fast spawn, short timer, more viruses
//
// MULTIPLAYER NOTE:
//   Runs SERVER side only.
//   Broadcast current tier to clients via ClientRpc for UI display.
// ============================================================

using UnityEngine;
using Scripts.Floor3.Gameplay;
using Scripts.ScriptableObjects;

namespace Scripts.Floor3.Core
{
    public enum DifficultyTier { Easy, Medium, Hard }

    public class DifficultyManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Scene References")]
        [SerializeField] private VirusSpawner _virusSpawner;
        [SerializeField] private QuizManager  _quizManager;
        [SerializeField] private RobotController _robotController;

        [Header("Virus Data Per Tier")]
        [SerializeField] private VirusData _virusDataEasy;
        [SerializeField] private VirusData _virusDataMedium;
        [SerializeField] private VirusData _virusDataHard;

        [Header("Spawn Intervals Per Tier (seconds)")]
        [SerializeField] private float _spawnIntervalEasy   = 10f;
        [SerializeField] private float _spawnIntervalMedium = 6f;
        [SerializeField] private float _spawnIntervalHard   = 3f;

        [Header("Quiz Time Limits Per Tier (seconds)")]
        [SerializeField] private float _quizTimeLimitEasy   = 25f;
        [SerializeField] private float _quizTimeLimitMedium = 18f;
        [SerializeField] private float _quizTimeLimitHard   = 12f;

        [Header("Proximity Thresholds")]
        [Tooltip("Distance from robot that counts as 'too far' for one player.")]
        [SerializeField] private float _farDistance = 6f;

        [Tooltip("Seconds both players must be far before triggering difficulty increase.")]
        [SerializeField] private float _farTimeTrigger = 4f;

        [Header("Wrong Answer Thresholds")]
        [SerializeField] private int _wrongAnswersForMedium = 2;
        [SerializeField] private int _wrongAnswersForHard   = 4;

        [Header("Debug")]
        [SerializeField] private bool _logDifficulty = true;

        // ── Private State ─────────────────────────────────────────────────

        private DifficultyTier _currentTier    = DifficultyTier.Easy;
        private int            _wrongAnswers   = 0;
        private float          _bothFarTimer   = 0f;
        private bool           _playersAreFar  = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Start()
        {
            ApplyTier(DifficultyTier.Easy);
        }

        // ── Public API (called by Floor3Brain / ProximityDetector) ────────

        /// <summary>Called by Floor3Brain.OnWrongAnswer()</summary>
        public void RegisterWrongAnswer()
        {
            _wrongAnswers++;
            Log($"Wrong answers: {_wrongAnswers}");
            EvaluateDifficulty();
        }

        /// <summary>Called by ProximityDetector each frame with current state</summary>
        public void UpdateProximity(bool bothPlayersFar)
        {
            if (bothPlayersFar)
            {
                _bothFarTimer += Time.deltaTime;
                if (!_playersAreFar && _bothFarTimer >= _farTimeTrigger)
                {
                    _playersAreFar = true;
                    Log($"Both players far for {_farTimeTrigger}s → difficulty pressure.");
                    EvaluateDifficulty();

                    // Alert robot emotion
                    // RobotStateMachine emotion is set via RobotController in ProximityDetector
                }
            }
            else
            {
                if (_bothFarTimer > 0f)
                    Log("Players returned close — proximity pressure lifted.");
                _bothFarTimer  = 0f;
                _playersAreFar = false;
                EvaluateDifficulty();
            }
        }

        // ── Difficulty Evaluation ─────────────────────────────────────────

        private void EvaluateDifficulty()
        {
            DifficultyTier newTier;

            if (_wrongAnswers >= _wrongAnswersForHard || (_playersAreFar && _wrongAnswers >= _wrongAnswersForMedium))
                newTier = DifficultyTier.Hard;
            else if (_wrongAnswers >= _wrongAnswersForMedium || _playersAreFar)
                newTier = DifficultyTier.Medium;
            else
                newTier = DifficultyTier.Easy;

            if (newTier != _currentTier)
                ApplyTier(newTier);
        }

        private void ApplyTier(DifficultyTier tier)
        {
            _currentTier = tier;
            Log($"── Difficulty → {tier} ──");

            switch (tier)
            {
                case DifficultyTier.Easy:
                    _virusSpawner?.SetVirusData(_virusDataEasy);
                    _virusSpawner?.SetContinuousInterval(_spawnIntervalEasy);
                    _quizManager?.SetTimeLimit(_quizTimeLimitEasy);
                    break;

                case DifficultyTier.Medium:
                    _virusSpawner?.SetVirusData(_virusDataMedium);
                    _virusSpawner?.SetContinuousInterval(_spawnIntervalMedium);
                    _quizManager?.SetTimeLimit(_quizTimeLimitMedium);
                    break;

                case DifficultyTier.Hard:
                    _virusSpawner?.SetVirusData(_virusDataHard);
                    _virusSpawner?.SetContinuousInterval(_spawnIntervalHard);
                    _quizManager?.SetTimeLimit(_quizTimeLimitHard);
                    break;
            }

            // Broadcast to UI (Day 5)
            // DifficultyEventBus.RaiseTierChanged(tier);
        }

        // ── Getters ───────────────────────────────────────────────────────

        public DifficultyTier CurrentTier  => _currentTier;
        public int WrongAnswerCount        => _wrongAnswers;
        public float FarTimer              => _bothFarTimer;
        public float FarDistance           => _farDistance;

        private void Log(string msg)
        {
            if (_logDifficulty) Debug.Log($"[DifficultyManager] {msg}");
        }
    }
}
