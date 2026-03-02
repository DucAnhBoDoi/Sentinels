using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Cài đặt quái")]
    public GameObject enemyPrefab; 
    public float spawnRate = 0.5f; 
    public bool canSpawn = true; 

    [Header("Danh sách 8 miệng ống")]
    public Transform[] spawnPoints; 

    private float nextSpawnTime;
    private bool hasStartedTimer = false; 

    void Update()
    {
        // Chỉ sinh quái nếu vẫn trong thời gian chơi (canSpawn = true)
        if (canSpawn && Time.time >= nextSpawnTime)
        {
            SpawnOneEnemy();
            nextSpawnTime = Time.time + spawnRate;
        }
    }

    void SpawnOneEnemy()
    {
        // KIỂM TRA LẦN CUỐI: Nếu đã thắng game thì không được sinh thêm bất cứ con nào
        if (!canSpawn || spawnPoints.Length == 0 || enemyPrefab == null) return; 

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedPipe = spawnPoints[randomIndex];

        // Sinh quái tại đúng vị trí miệng ống
        Instantiate(enemyPrefab, selectedPipe.position, Quaternion.identity);

        // KÍCH HOẠT ĐỒNG HỒ ĐẾM NGƯỢC
        if (!hasStartedTimer)
        {
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.StartTimer();
                hasStartedTimer = true; 
            }
        }
    }

    // Hàm để GameManager gọi khi thắng trận
    public void StopSpawning()
    {
        canSpawn = false; // Ngắt điều kiện sinh quái ngay lập tức
        Debug.Log("Spawner đã dừng! Không quái nào được sinh ra nữa.");
    }
}