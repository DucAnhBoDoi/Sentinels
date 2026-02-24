using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; 

    [Header("Cài đặt thời gian (Giây)")]
    public float spawnRate = 60f; // 1 phút mới ra một đợt

    [Header("Cài đặt số lượng")]
    public int startEnemies = 2; // Chỉ ra đúng 2 con
    public int enemiesIncrease = 1; 

    private float timer;

    void Start()
    {
        // Bắt đầu với số âm để đợt đầu tiên ra cực kỳ chậm
        timer = -10f; 
        Debug.Log("Hệ thống Spawner: Đang chờ đợt quái đầu tiên...");
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            SpawnWave();
            timer = 0; 
        }
    }

    void SpawnWave()
    {
        if (enemyPrefab == null) return;
        
        // Sinh quái ở vị trí rất xa (khoảng cách 35) để chúng bò vào lâu hơn
        Vector2 spawnDirection = Random.insideUnitCircle.normalized;
        Vector2 baseSpawnPos = spawnDirection * 35f; 

        for (int i = 0; i < startEnemies; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 2f; 
            Vector3 finalPos = new Vector3(baseSpawnPos.x + offset.x, baseSpawnPos.y + offset.y, 0);
            Instantiate(enemyPrefab, finalPos, Quaternion.identity);
        }
        
        Debug.Log("Một nhóm quái nhỏ đã xuất hiện ở xa.");
    }
}