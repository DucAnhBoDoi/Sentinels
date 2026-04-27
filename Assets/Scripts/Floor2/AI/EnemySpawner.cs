using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Cài đặt quái")]
    public GameObject enemyPrefab; 
    public bool canSpawn = true; 

    [Header("Spawn Rate (càng lớn = càng chậm)")]
    public float rateTutorial = 4.5f; // phút 7–4 (rất chậm)
    public float rateEasy = 4.0f;     // phút 4–3
    public float rateHard = 3.5f;     // phút 3 (ít quái)
    public float rateInsane = 3.0f;   // phút 2 (không spam)

    [Header("Giới hạn số lượng")]
    public int maxEnemies = 3; // 🔥 luôn giữ ít

    [Header("Spawn Points")]
    public Transform[] spawnPoints; 

    [Header("Spawn Anti-Overlap")]
    public float spawnRadius = 1.5f;
    public float minSpawnDistance = 1.2f;

    [Header("Anti Spam")]
    public float minSpawnInterval = 2.5f; // 🔥 chậm rõ rệt

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
        if (fm == null || !fm.timerIsRunning) return;

        currentEnemies.RemoveAll(e => e == null);

        if (currentEnemies.Count >= maxEnemies) return;

        float timeRemaining = fm.timeRemaining;
        float currentSpawnRate;

        // ===== GIAI ĐOẠN =====

        if (timeRemaining <= 120f) // 🔴 PHÚT 2
        {
            currentDamageMultiplier = 1.8f;
            currentSpeedMultiplier = 0.9f; // 🔥 CHẬM LẠI

            currentSpawnRate = rateInsane;
        }
        else if (timeRemaining <= 180f) // 🟡 PHÚT 3
        {
            currentDamageMultiplier = 1.2f;
            currentSpeedMultiplier = 1.1f; // 🔥 chỉ hơi nhanh

            currentSpawnRate = rateHard;
        }
        else if (timeRemaining <= 240f)
        {
            currentDamageMultiplier = 1.0f;
            currentSpeedMultiplier = 1.05f;

            currentSpawnRate = rateEasy;
        }
        else
        {
            currentDamageMultiplier = 1.0f;
            currentSpeedMultiplier = 1.0f;

            currentSpawnRate = rateTutorial;
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

        // 🔥 GIẢM TỐC NGAY TỪ LÚC SPAWN (QUAN TRỌNG NHẤT)
        Rigidbody2D rb = newEnemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            rb.linearVelocity = randomDir * Random.Range(0.5f, 1.0f); // 🔥 CHẬM HƠN NHIỀU
        }

        BoidEnemy boid = newEnemy.GetComponent<BoidEnemy>();

        if (boid != null)
        {
            boid.attackDamage *= currentDamageMultiplier;
            boid.speed *= currentSpeedMultiplier;

            SpriteRenderer sr = newEnemy.GetComponent<SpriteRenderer>();

            if (currentDamageMultiplier > 1.5f)
            {
                if (sr != null) sr.color = Color.red; // phút 2
            }
            else if (currentSpeedMultiplier > 1.05f)
            {
                if (sr != null) sr.color = Color.yellow; // phút 3
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

                float dist = Vector2.Distance(randomPos, enemy.transform.position);

                if (dist < minSpawnDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
                return randomPos;
        }

        return center + Random.insideUnitCircle * spawnRadius;
    }

    public void StopSpawning()
    {
        canSpawn = false;
    }
}