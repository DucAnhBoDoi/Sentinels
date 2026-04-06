// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/VirusSpawner.cs
// Namespace: Scripts.Floor3.Gameplay
// ── REWRITTEN ──────────────────────────────────────────────
// KEY CHANGES:
//   1. CONTINUOUS SPAWN: A background loop spawns viruses
//      periodically from game start. This is the baseline
//      pressure that always exists.
//
//   2. WRONG ANSWER SPAWN: When quiz answer is wrong,
//      SpawnWave() is called. It picks spawn points NEAREST
//      to the checkpoint position (not random map-wide).
//      This makes wrong-answer punishment feel local and fair.
//
//   3. MAX VIRUS CAP: Continuous spawn respects a max alive
//      cap so the scene doesn't get overwhelmed.
//
// SPAWN POINT STRATEGY:
//   You place spawn points around your entire map.
//   - Continuous spawn: truly random across all points
//   - Wrong answer spawn: filtered to N nearest points
//     to the checkpoint where the wrong answer happened
//
// MULTIPLAYER NOTE:
//   Only Server runs spawn loops.
//   Wrap StartCoroutine calls with if (!IsServer) return;
// ============================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Floor3.AI;
using Scripts.ScriptableObjects;

namespace Scripts.Floor3.Gameplay
{
    public class VirusSpawner : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private GameObject _virusPrefab;
        [SerializeField] private Transform _virusContainer;
        [SerializeField] private RobotController _robotController;

        [Header("All Spawn Points")]
        [Tooltip("Place these around the entire map — at corridor entrances,\n" +
                 "room edges, etc. Both continuous and wrong-answer spawns use this list.")]
        [SerializeField] private Transform[] _spawnPoints;

        [Header("Virus Configuration")]
        [SerializeField] private VirusData _currentVirusData;

        [Header("Continuous Spawn Settings")]
        [Tooltip("Seconds between each automatically spawned virus during normal gameplay.")]
        [SerializeField] private float _continuousSpawnInterval = 8f;

        [Tooltip("Maximum viruses alive at any time. Continuous spawn pauses above this.")]
        [SerializeField] private int _maxActiveViruses = 10;

        [Tooltip("How many viruses spawn per continuous tick (usually 1).")]
        [SerializeField] private int _continuousSpawnCount = 1;

        [Header("Wrong Answer Spawn Settings")]
        [Tooltip("How many of the NEAREST spawn points to use for wrong-answer spawns.\n" +
                 "E.g. 2 = pick from the 2 spawn points closest to the failed checkpoint.")]
        [SerializeField] private int _nearestSpawnPointCount = 2;

        [Header("Robot Distance Spawn Limit")]
        [Tooltip("Minimum distance from robot where virus can spawn.")]
        [SerializeField] private float _minSpawnDistanceFromRobot = 4f;

        [Tooltip("Maximum distance from robot where virus can spawn.")]
        [SerializeField] private float _maxSpawnDistanceFromRobot = 15f;

        [Header("Debug")]
        [SerializeField] private bool _logSpawning = true;

        // ── Private State ─────────────────────────────────────────────────

        private readonly List<VirusAI> _activeViruses = new List<VirusAI>();
        private int _totalWavesSpawned = 0;
        private bool _spawningActive = false;
        private Coroutine _continuousLoop = null;

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            if (_virusPrefab == null) Debug.LogError("[VirusSpawner] Virus prefab not assigned!");
            if (_robotController == null) Debug.LogError("[VirusSpawner] RobotController not assigned!");
            if (_virusContainer == null) Debug.LogError("[VirusSpawner] VirusContainer not assigned!");
            if (_currentVirusData == null) Debug.LogError("[VirusSpawner] VirusData not assigned!");
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                Debug.LogWarning("[VirusSpawner] No spawn points assigned!");
        }

        private void Start()
        {
            StartContinuousSpawn();
        }

        // ── Continuous Spawn ─────────────────────────────────────────────

        /// <summary>
        /// Starts the background virus spawn loop.
        /// Runs for the entire level — provides constant pressure.
        /// </summary>
        public void StartContinuousSpawn()
        {
            if (_spawningActive) return;
            _spawningActive = true;
            _continuousLoop = StartCoroutine(ContinuousSpawnLoop());
            Log("Continuous spawn started.");
        }

        public void StopContinuousSpawn()
        {
            _spawningActive = false;
            if (_continuousLoop != null)
            {
                StopCoroutine(_continuousLoop);
                _continuousLoop = null;
            }
            Log("Continuous spawn stopped.");
        }

        private IEnumerator ContinuousSpawnLoop()
        {
            // Small initial delay so the level can initialize fully
            yield return new WaitForSeconds(3f);

            while (_spawningActive)
            {
                if (_activeViruses.Count < _maxActiveViruses)
                {
                    Log($"Continuous tick — spawning {_continuousSpawnCount} virus(es). " +
                        $"Active: {_activeViruses.Count}/{_maxActiveViruses}");

                    for (int i = 0; i < _continuousSpawnCount; i++)
                    {
                        Vector3 spawnPos = GetRandomSpawnPoint();
                        SpawnSingleVirus(spawnPos);
                        if (i < _continuousSpawnCount - 1)
                            yield return new WaitForSeconds(_currentVirusData.SpawnInterval);
                    }
                }
                else
                {
                    Log($"Max virus cap reached ({_maxActiveViruses}). Waiting...");
                }

                yield return new WaitForSeconds(_continuousSpawnInterval);
            }
        }

        // ── Wrong Answer Spawn ────────────────────────────────────────────

        /// <summary>
        /// Spawn a punishment wave near the checkpoint where the wrong answer happened.
        /// checkpointPosition = the world position of that checkpoint waypoint.
        /// </summary>
        public void SpawnWave(Vector3 checkpointPosition)
        {
            if (_currentVirusData == null) return;

            _totalWavesSpawned++;
            int count = _currentVirusData.SpawnCountPerWave;

            Log($"Wrong answer wave #{_totalWavesSpawned} — {count} viruses near checkpoint at {checkpointPosition}");
            StartCoroutine(SpawnWaveNearCheckpoint(checkpointPosition, count));
        }

        /// <summary>
        /// Overload without position — falls back to random spawn points.
        /// Used as fallback if checkpoint position is unavailable.
        /// </summary>
        public void SpawnWave()
        {
            if (_robotController != null)
                SpawnWave(_robotController.transform.position);
            else
                SpawnWave(transform.position);
        }

        private IEnumerator SpawnWaveNearCheckpoint(Vector3 checkpointPos, int count)
        {
            // Get the N nearest spawn points to the checkpoint
            List<Transform> nearestPoints = GetNearestSpawnPoints(checkpointPos, _nearestSpawnPointCount);

            for (int i = 0; i < count; i++)
            {
                // Pick from nearest points only
                Vector3 spawnPos = nearestPoints.Count > 0
                    ? nearestPoints[Random.Range(0, nearestPoints.Count)].position
                    : GetRandomSpawnPoint();

                SpawnSingleVirus(spawnPos);

                if (i < count - 1)
                    yield return new WaitForSeconds(_currentVirusData.SpawnInterval);
            }
        }

        // ── Spawn Logic ───────────────────────────────────────────────────

        private void SpawnSingleVirus(Vector3 position)
        {
            if (_virusPrefab == null || _currentVirusData == null) return;

            GameObject virusGO = Instantiate(_virusPrefab, position, Quaternion.identity, _virusContainer);
            virusGO.name = $"Virus_{_activeViruses.Count + 1}";

            VirusAI virusAI = virusGO.GetComponent<VirusAI>();
            if (virusAI == null)
            {
                Debug.LogError("[VirusSpawner] Virus prefab is missing VirusAI component!");
                Destroy(virusGO);
                return;
            }

            virusAI.Initialize(_currentVirusData, _robotController);
            _activeViruses.Add(virusAI);
            StartCoroutine(WatchVirusDeath(virusAI));

            Log($"Spawned virus at {position}. Total active: {_activeViruses.Count}");
        }

        private IEnumerator WatchVirusDeath(VirusAI virus)
        {
            while (virus != null)
                yield return new WaitForSeconds(0.2f);
            _activeViruses.Remove(virus);
        }

        // ── Spawn Point Selection ─────────────────────────────────────────

        private Vector3 GetRandomSpawnPoint()
        {
            // NEW: try spawn points near robot first
            List<Transform> nearbyPoints = GetSpawnPointsNearRobot();

            if (nearbyPoints.Count > 0)
                return nearbyPoints[Random.Range(0, nearbyPoints.Count)].position;

            // fallback to original behavior
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return transform.position;

            return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
        }

        /// <summary>
        /// Returns the N spawn points closest to a given world position.
        /// Used to localize wrong-answer spawns near the failed checkpoint.
        /// </summary>
        private List<Transform> GetNearestSpawnPoints(Vector3 origin, int count)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return new List<Transform>();

            return _spawnPoints
                .Where(sp => sp != null)
                .OrderBy(sp => Vector3.Distance(sp.position, origin))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Returns spawn points within a distance range from the escort robot.
        /// Ensures viruses do not spawn too close to the robot.
        /// </summary>
        private List<Transform> GetSpawnPointsNearRobot()
        {
            if (_robotController == null || _spawnPoints == null)
                return new List<Transform>();

            Vector3 robotPos = _robotController.transform.position;

            return _spawnPoints
                .Where(sp =>
                    sp != null &&
                    Vector3.Distance(sp.position, robotPos) >= _minSpawnDistanceFromRobot &&
                    Vector3.Distance(sp.position, robotPos) <= _maxSpawnDistanceFromRobot
                )
                .ToList();
        }

        // ── Public API ────────────────────────────────────────────────────

        public void SetVirusData(VirusData newData)
        {
            _currentVirusData = newData;
            Log($"VirusData updated to: {newData.name}");
        }

        public void SetContinuousInterval(float interval)
        {
            _continuousSpawnInterval = interval;
            // Restart loop to apply new interval immediately
            StopContinuousSpawn();
            StartContinuousSpawn();
        }

        public void ClearAllViruses()
        {
            StopContinuousSpawn();
            foreach (var v in _activeViruses)
                if (v != null) Destroy(v.gameObject);
            _activeViruses.Clear();
            Log("All viruses cleared.");
        }

        // ── Getters (for DifficultyManager Day 4) ────────────────────────

        public int GetActiveVirusCount() => _activeViruses.Count;
        public int GetTotalWavesSpawned() => _totalWavesSpawned;

        // ── Helpers ───────────────────────────────────────────────────────

        private void Log(string msg)
        {
            if (_logSpawning) Debug.Log($"[VirusSpawner] {msg}");
        }

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (_spawnPoints == null) return;
            foreach (var sp in _spawnPoints)
            {
                if (sp == null) continue;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(sp.position, 0.3f);
            }
        }
    }
}