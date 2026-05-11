// ============================================================
// FILE: Assets/Scripts/Floor3/Core/GameContext.cs
// Namespace: Scripts.Floor3.Core
// ────────────────────────────────────────────────────
// Runtime snapshot of everything happening in Floor 3.
// Used by:
//   - Floor3Brain: update values on events
//   - DifficultyManager: read context to tune difficulty
//   - GameOverController: display final stats
//
// PATTERN: Simple data container + events.
//   NOT a God Object — it stores, not decides.
//   Decision logic stays in DifficultyManager / Floor3Brain.
//
// WHY SINGLETON HERE?
//   GameContext is read by many unrelated systems (UI, LLM,
//   Difficulty). Passing it via Inspector to every script
//   creates inspector hell. Singleton is justified when:
//     1. There is exactly one instance (one level)
//     2. Many systems need read access
//     3. No game logic lives inside it
//   All three apply here.
//
// MULTIPLAYER NOTE:
//   Replace Singleton with a NetworkBehaviour.
//   Values become NetworkVariables synced to all clients.
// ============================================================

using System;
using UnityEngine;

namespace Scripts.Floor3.Core
{
    public class GameContext : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────

        public static GameContext Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Events (fired when values change — UI / LLM subscribe) ───────

        public static event Action<GameOverReason> OnGameOver;
        public static event Action                 OnLevelComplete;
        public static event Action<GameContext>    OnContextUpdated; // for LLM Day 6

        // ── Tracked State (read-only outside, set via methods) ────────────

        // Robot
        public float RobotHpNormalized   { get; private set; } = 1f;
        public RobotEmotion RobotEmotion { get; private set; } = RobotEmotion.Stable;

        // Quiz
        public int WrongAnswerCount      { get; private set; } = 0;
        public int CorrectAnswerCount    { get; private set; } = 0;
        public int CheckpointsReached    { get; private set; } = 0;
        public int TotalCheckpoints      { get; private set; } = 5; // set from Inspector or RobotController

        // Difficulty
        public DifficultyTier CurrentDifficulty { get; private set; } = DifficultyTier.Easy;

        // Time
        public float TimeElapsed         { get; private set; } = 0f;
        private bool _trackingTime       = false;

        // Level outcome
        public bool IsGameOver           { get; private set; } = false;
        public bool IsLevelComplete      { get; private set; } = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Update()
        {
            if (_trackingTime && !IsGameOver && !IsLevelComplete)
                TimeElapsed += Time.deltaTime;
        }

        // ── Public API (called by Floor3Brain) ────────────────────────────

        public void StartTracking()
        {
            _trackingTime = true;
            TimeElapsed   = 0f;
        }

        public void UpdateRobotHp(float normalizedHp)
        {
            RobotHpNormalized = normalizedHp;
            NotifyUpdated();
        }

        public void UpdateEmotion(RobotEmotion emotion)
        {
            RobotEmotion = emotion;
            NotifyUpdated();
        }

        public void RegisterCorrectAnswer()
        {
            CorrectAnswerCount++;
            NotifyUpdated();
        }

        public void RegisterWrongAnswer()
        {
            WrongAnswerCount++;
            NotifyUpdated();
        }

        public void RegisterCheckpoint()
        {
            CheckpointsReached++;
            NotifyUpdated();
        }

        public void UpdateDifficulty(DifficultyTier tier)
        {
            CurrentDifficulty = tier;
            NotifyUpdated();
        }

        public void SetTotalCheckpoints(int total)
        {
            TotalCheckpoints = total;
        }

        // ── Win / Lose Triggers ───────────────────────────────────────────

        public void TriggerGameOver(GameOverReason reason)
        {
            if (IsGameOver || IsLevelComplete) return;
            IsGameOver    = true;
            _trackingTime = false;
            Debug.Log($"[GameContext] GAME OVER — Reason: {reason}");
            OnGameOver?.Invoke(reason);
        }

        public void TriggerLevelComplete()
        {
            if (IsGameOver || IsLevelComplete) return;
            IsLevelComplete = true;
            _trackingTime   = false;
            Debug.Log("[GameContext] LEVEL COMPLETE!");
            OnLevelComplete?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private void NotifyUpdated() => OnContextUpdated?.Invoke(this);

        /// <summary>Human-readable summary — used by LLM prompt builder Day 6.</summary>
        public string ToPromptSummary()
        {
            return $"Robot HP: {RobotHpNormalized:P0}, " +
                   $"Emotion: {RobotEmotion}, " +
                   $"Wrong answers: {WrongAnswerCount}, " +
                   $"Checkpoints: {CheckpointsReached}/{TotalCheckpoints}, " +
                   $"Difficulty: {CurrentDifficulty}, " +
                   $"Time: {TimeElapsed:F0}s";
        }
    }

    public enum GameOverReason
    {
        RobotDestroyed, // Robot HP = 0
        TimeOut         // Future: time limit exceeded
    }
}
