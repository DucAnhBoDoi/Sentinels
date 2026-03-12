// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/DebugVirusKiller.cs
// Namespace: Scripts.Floor3.Gameplay
// ------------------------------------------------------------
// DEBUG COMPONENT — Remove or disable before final build.
//
// Attach to Player_A_Navigator and Player_B_Mechanic.
// When a player's collider touches a virus, the virus dies
// instantly. Lets you clear viruses quickly during testing
// without needing a full attack system (Day 5).
//
// WHY A SEPARATE COMPONENT (not inside PlayerMovement)?
//   - Keeps debug logic isolated — easy to find and remove
//   - PlayerMovement.cs is existing Floor1 code — we don't
//     touch it more than necessary
//   - This component is self-contained and harmless when removed
//
// HOW TO USE:
//   Add Component → DebugVirusKiller on each player GameObject
//   Press Play → walk into virus → it disappears
//   Remove component when Day 5 real attack system is ready
// ============================================================

using UnityEngine;
using Scripts.Floor3.AI;

namespace Scripts.Floor3.Gameplay
{
    public class DebugVirusKiller : MonoBehaviour
    {
        [Header("Debug Settings")]
        [Tooltip("Uncheck to disable kill-on-touch without removing component.")]
        [SerializeField] private bool _enabled = true;

        [Tooltip("Log to console when a virus is killed by touch.")]
        [SerializeField] private bool _logKills = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_enabled) return;

            VirusAI virus = other.GetComponent<VirusAI>();
            if (virus != null)
            {
                if (_logKills)
                    Debug.Log($"[DebugVirusKiller] {gameObject.name} touched virus → killed.");

                // Call public TakeDamage with huge value — guaranteed one-shot kill
                virus.TakeDamage(9999f);
            }
        }

        // Also catch overlap if player is already inside a virus when it spawns
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_enabled) return;

            VirusAI virus = other.GetComponent<VirusAI>();
            if (virus != null)
                virus.TakeDamage(9999f);
        }
    }
}
