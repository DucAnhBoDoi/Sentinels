// ============================================================
// FILE: Assets/Scripts/Floor3/Core/ProximityEventBus.cs
// Namespace: Scripts.Floor3.Core
// ────────────────────────────────────────────────────
// Event bus for proximity data → UI warning system.
// ProximityDetector raises → ProximityHUDController listens.
// Same pattern as RobotEventBus and QuizEventBus.
// ============================================================

using System;
using UnityEngine;

namespace Scripts.Floor3.Core
{
    public static class ProximityEventBus
    {
        // Fires every Update with current distances
        // (distA, distB, warnThreshold, farThreshold)
        public static event Action<float, float, float, float> OnProximityUpdated;

        public static void RaiseProximityUpdated(
            float distA, float distB, float warnThreshold, float farThreshold)
        {
            OnProximityUpdated?.Invoke(distA, distB, warnThreshold, farThreshold);
        }
    }
}
