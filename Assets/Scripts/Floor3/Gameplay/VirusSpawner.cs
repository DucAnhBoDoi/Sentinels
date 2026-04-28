// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/VirusSpawner.cs
// Namespace: Scripts.Floor3.Gameplay
// ── MULTIPLAYER READY ──────────────────────────────────────
// KEY CHANGES:
//   1. Inherits from NetworkBehaviour instead of MonoBehaviour.
//   2. Uses IsServer checks to ensure ONLY the Host spawns viruses.
//   3. Uses NetworkObject.Spawn() to synchronize viruses to clients.
//   4. Uses NetworkObject.Despawn() to clean them up properly over the network.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG
using Scripts.Floor3.AI;
using Scripts.ScriptableObjects;

namespace Scripts.Floor3.Gameplay
{
    // ĐỔI TỪ MonoBehaviour SANG NetworkBehaviour
    public class VirusSpawner : NetworkBehaviour
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
            // CHỈ MÁY CHỦ (HOST) MỚI ĐƯỢC CHẠY VÒNG LẶP ĐẺ QUÁI
            if (IsServer)
            {
                StartContinuousSpawn();
            }
        }

        // ── Continuous Spawn ─────────────────────────────────────────────

        public void StartContinuousSpawn()
        {
            // CHẶN CLIENT KHÔNG CHO CHẠY
            if (!IsServer) return; 

            if (_spawningActive) return;
            _spawningActive = true;
            _continuousLoop = StartCoroutine(ContinuousSpawnLoop());
            Log("Continuous spawn started.");
        }

        public void StopContinuousSpawn()
        {
            // CHẶN CLIENT KHÔNG CHO CHẠY
            if (!IsServer) return;

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

        public void SpawnWave(Vector3 checkpointPosition)
        {
            // CHẶN CLIENT
            if (!IsServer || _currentVirusData == null) return;

            _totalWavesSpawned++;
            int count = _currentVirusData.SpawnCountPerWave;

            Log($"Wrong answer wave #{_totalWavesSpawned} — {count} viruses near checkpoint at {checkpointPosition}");
            StartCoroutine(SpawnWaveNearCheckpoint(checkpointPosition, count));
        }

        public void SpawnWave()
        {
            if (!IsServer) return;

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

            // LỆNH MẠNG: ĐẨY QUÁI VỪA TẠO LÊN CHO TẤT CẢ CÁC CLIENT CÙNG THẤY
            NetworkObject netObj = virusGO.GetComponent<NetworkObject>();
            if (netObj != null) 
            {
                netObj.Spawn(true);
            }
            else 
            {
                Debug.LogError("[VirusSpawner] Thiếu NetworkObject trên Prefab Virus!");
            }

            VirusAI virusAI = virusGO.GetComponent<VirusAI>();
            if (virusAI == null)
            {
                Debug.LogError("[VirusSpawner] Virus prefab is missing VirusAI component!");
                // Hủy đúng chuẩn mạng
                if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
                else Destroy(virusGO);
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
            // CHỈ SERVER ĐƯỢC XÓA QUÁI TRÊN MẠNG
            if (!IsServer) return; 

            StopContinuousSpawn();
            foreach (var v in _activeViruses)
            {
                if (v != null)
                {
                    // XÓA CHUẨN MẠNG: Dùng Despawn thay vì Destroy
                    NetworkObject netObj = v.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned)
                    {
                        netObj.Despawn(true);
                    }
                    else
                    {
                        Destroy(v.gameObject);
                    }
                }
            }
            _activeViruses.Clear();
            Log("All viruses cleared.");
        }

        // ── Getters ───────────────────────────────────────────────────────

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