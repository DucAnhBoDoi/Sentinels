// ============================================================
// FILE: Assets/Scripts/Floor3/Core/RobotEventBus.cs
// Namespace: Scripts.Floor3.Core
// ------------------------------------------------------------
// Central event bus for all robot-related events.
// WHY: Decouples Robot → Brain → UI → Quiz without direct references.
//      Any system can subscribe without creating tight coupling.
// PATTERN: Static events (simple for single-player).
// MULTIPLAYER NOTE: Replace static events with RPCs or
//                   NetworkVariable observers when adding Netcode.
//                   Keep event signatures identical — only the
//                   transport layer changes.
// ============================================================

using System;
using UnityEngine;

namespace Scripts.Floor3.Core
{
    public static class RobotEventBus
    {
        // Fired when the robot reaches a waypoint checkpoint
        // int = waypoint index
        public static event Action<int> OnCheckpointReached;

        // Fired when robot state changes
        public static event Action<RobotState> OnStateChanged;

        // Fired when robot emotion changes
        public static event Action<RobotEmotion> OnEmotionChanged;

        // Fired when robot takes damage
        // float = current HP (0–1 normalized)
        public static event Action<float> OnRobotDamaged;

        // Fired when robot HP reaches zero
        public static event Action OnRobotDied;

        // Fired when robot reaches final waypoint (level complete)
        public static event Action OnEscortComplete;

        // ── Invokers (called only by RobotController / RobotStateMachine) ──

        public static void RaiseCheckpointReached(int waypointIndex)
        {
            Debug.Log($"[RobotEventBus] Checkpoint reached: {waypointIndex}");
            OnCheckpointReached?.Invoke(waypointIndex);
        }

        public static void RaiseStateChanged(RobotState newState)
        {
            Debug.Log($"[RobotEventBus] State → {newState}");
            OnStateChanged?.Invoke(newState);
        }

        public static void RaiseEmotionChanged(RobotEmotion newEmotion)
        {
            Debug.Log($"[RobotEventBus] Emotion → {newEmotion}");
            OnEmotionChanged?.Invoke(newEmotion);
        }

        public static void RaiseRobotDamaged(float normalizedHp)
        {
            OnRobotDamaged?.Invoke(normalizedHp);
        }

        public static void RaiseRobotDied()
        {
            Debug.Log("[RobotEventBus] Robot has died.");
            OnRobotDied?.Invoke();
        }

        public static void RaiseEscortComplete()
        {
            Debug.Log("[RobotEventBus] Escort complete!");
            OnEscortComplete?.Invoke();
        }
    }
}
