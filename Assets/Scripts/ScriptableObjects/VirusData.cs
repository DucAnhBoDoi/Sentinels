// ============================================================
// FILE: Assets/Scripts/ScriptableObjects/VirusData.cs
// Namespace: Scripts.ScriptableObjects
// ------------------------------------------------------------
// ScriptableObject for virus configuration.
// WHY ScriptableObject HERE (unlike QuizQuestion)?
//   Virus stats ARE authored design-time data — designers
//   want to tune speed/HP/damage in the Inspector without
//   touching code. Perfect ScriptableObject use case.
//
// Create multiple assets:
//   VirusData_Easy.asset   → slow, low HP, low damage
//   VirusData_Medium.asset → balanced
//   VirusData_Hard.asset   → fast, tanky, high damage
//
// DifficultyManager (Day 4) will swap which asset is active.
//
// HOW TO CREATE:
//   Right-click in Project → Create → Floor3 → VirusData
// ============================================================

using UnityEngine;

namespace Scripts.ScriptableObjects
{
    [CreateAssetMenu(fileName = "VirusData", menuName = "Floor3/VirusData")]
    public class VirusData : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Units per second toward the robot")]
        public float MoveSpeed = 2.5f;

        [Header("Combat")]
        [Tooltip("Damage dealt to robot on contact")]
        public float DamageOnContact = 10f;

        [Tooltip("Seconds between damage ticks while touching robot")]
        public float DamageCooldown = 1f;

        [Tooltip("Virus HP — how many hits from Player B to kill")]
        public float MaxHp = 1f;

        [Header("Spawn")]
        [Tooltip("How many viruses spawn per wrong answer wave")]
        public int SpawnCountPerWave = 3;

        [Tooltip("Delay in seconds between each virus spawn in a wave")]
        public float SpawnInterval = 0.3f;
    }
}
