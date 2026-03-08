// ============================================================
// FILE: Assets/Scripts/Floor3/Core/RobotState.cs
// Namespace: Scripts.Floor3.Core
// ------------------------------------------------------------
// Pure data enum. No MonoBehaviour. No dependencies.
// Shared by RobotStateMachine, Floor3Brain, UI systems.
// MULTIPLAYER NOTE: This enum will be synced via NetworkVariable<RobotState>
//                   when Netcode is integrated (Day 7+).
// ============================================================

namespace Scripts.Floor3.Core
{
    public enum RobotState
    {
        Moving,         // Traveling toward next waypoint
        Waiting,        // Paused at waypoint, waiting for quiz or players
        AskingQuestion, // Quiz is active
        Stunned,        // Wrong answer penalty — robot halted
        Accelerated     // Speed boost after correct answer
    }

    public enum RobotEmotion
    {
        Stable,     // Default — all good
        Confused,   // Players are far or slow to answer
        Panicked    // Low HP or virus swarm nearby
    }
}
