using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG

public class EnemySpawner : NetworkBehaviour
{
    [Header("Cài đặt quái")]
    public GameObject enemyPrefab; 
    public bool canSpawn = true; 

    [Header("Spawn Rate (càng lớn = càng chậm)")]
    public float rateTutorial = 4.5f; 
    public float rateEasy = 4.0f;    
    public float rateHard = 3.5f;    
    public float rateInsane = 3.0f;  

    [Header("Giới hạn số lượng")]
    public int maxEnemies = 3; 

    [Header("Spawn Points")]
    public Transform[] spawnPoints; 

    [Header("Spawn Anti-Overlap")]
    public float spawnRadius = 1.5f;
    public float minSpawnDistance = 1.2f;

    [Header("Anti Spam")]
    public float minSpawnInterval = 2.5f; 

    private float spawnTimer = 0f;
    private Floor2Manager fm;

    private float currentDamageMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;

    private List<GameObject> currentEnemies = new List<GameObject>();

    void Start()
    {
        fm = Object.FindAnyObjectByType<Floor2Manager>();
    }

    void Update()
    {
        // CHỈ SERVER MỚI ĐƯỢC CHẠY TIMER VÀ ĐẺ QUÁI
        if (!IsServer || fm == null || !fm.timerIsRunning.Value) return;

        currentEnemies.RemoveAll(e => e == null);
        if (currentEnemies.Count >= maxEnemies) return;

        float timeRemaining = fm.timeRemaining.Value;
        float currentSpawnRate;

        if (timeRemaining <= 120f) 
        {
            currentDamageMultiplier = 1.8f; currentSpeedMultiplier = 0.9f; currentSpawnRate = rateInsane;
        }
        else if (timeRemaining <= 180f) 
        {
            currentDamageMultiplier = 1.2f; currentSpeedMultiplier = 1.1f; currentSpawnRate = rateHard;
        }
        else if (timeRemaining <= 240f)
        {
            currentDamageMultiplier = 1.0f; currentSpeedMultiplier = 1.05f; currentSpawnRate = rateEasy;
        }
        else
        {
            currentDamageMultiplier = 1.0f; currentSpeedMultiplier = 1.0f; currentSpawnRate = rateTutorial;
        }

        float finalRate = Mathf.Max(currentSpawnRate, minSpawnInterval);
        spawnTimer += Time.deltaTime;

        if (canSpawn && spawnTimer >= finalRate)
        {
            SpawnSingle();
            spawnTimer = 0f;
        }
    }

    void SpawnSingle()
    {
        if (!canSpawn || spawnPoints.Length == 0 || enemyPrefab == null) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedPipe = spawnPoints[randomIndex];

        Vector2 spawnPos = GetValidSpawnPosition(selectedPipe.position);

        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        currentEnemies.Add(newEnemy);

        Rigidbody2D rb = newEnemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            rb.linearVelocity = randomDir * Random.Range(0.5f, 1.0f);
        }

        BoidEnemy boid = newEnemy.GetComponent<BoidEnemy>();
        if (boid != null)
        {
            boid.attackDamage *= currentDamageMultiplier;
            boid.speed *= currentSpeedMultiplier;
        }

        // 1. GỌI LỆNH SPAWN LÊN MẠNG ĐỂ CLIENT THẤY QUÁI
        newEnemy.GetComponent<NetworkObject>().Spawn(true);

        // 2. BÁO CHO MỌI NGƯỜI ĐỔI MÀU QUÁI THEO GIAI ĐOẠN 
        // (Đã thay SetColorClientRpc thành SetColor để đồng bộ BaseColor)
        if (boid != null)
        {
            if (currentDamageMultiplier > 1.5f)
            {
                boid.SetColor(Color.red); // phút 2
            }
            else if (currentSpeedMultiplier > 1.05f)
            {
                boid.SetColor(Color.yellow); // phút 3
            }
        }
    }

    Vector2 GetValidSpawnPosition(Vector2 center)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomPos = center + Random.insideUnitCircle * spawnRadius;
            bool tooClose = false;
            foreach (GameObject enemy in currentEnemies)
            {
                if (enemy == null) continue;
                if (Vector2.Distance(randomPos, enemy.transform.position) < minSpawnDistance)
                {
                    tooClose = true; break;
                }
            }
            if (!tooClose) return randomPos;
        }
        return center + Random.insideUnitCircle * spawnRadius;
    }

    public void StopSpawning() { canSpawn = false; }
}