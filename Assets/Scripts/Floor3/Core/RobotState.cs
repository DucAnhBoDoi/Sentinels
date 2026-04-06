// ============================================================
// FILE: Assets/Scripts/Floor3/Core/RobotState.cs
// Namespace: Scripts.Floor3.Core
// ── UPDATED ────────────────────────────────────────────────
// Added: Panicked to RobotState enum
//   Panicked = virus is within panic radius → robot freezes
//   Different from Stunned (wrong answer timer)
//   Different from RobotEmotion.Panicked (visual/audio state)
//
// RobotState    = MOVEMENT behavior (what the robot does)
// RobotEmotion  = VISUAL/AUDIO state (how the robot looks/sounds)
// They are independent — robot can be Moving + Emotionally Panicked
// ============================================================

namespace Scripts.Floor3.Core
{
    public enum RobotState
    {
        Moving,         // Traveling toward next waypoint
        Waiting,        // Paused at waypoint, waiting for quiz or players
        AskingQuestion, // Quiz is active
        Stunned,        // Wrong answer penalty — robot halted for N seconds
        Accelerated,    // Speed boost after correct answer
        Panicked        // Virus within panic radius — robot frozen until area clear
    }

    public enum RobotEmotion
    {
        Stable,     // Default — all good
        Confused,   // Players are far or slow to answer
        Panicked    // Low HP or virus swarm nearby — visual state only
    }
}