using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Cài đặt quái")]
    public GameObject enemyPrefab; 
    public bool canSpawn = true; 

    [Header("Độ khó tăng dần (Dựa trên 5 phút)")]
    public float initialSpawnRate = 2.5f; // Lúc đầu: 2.5 giây/con (Ra rất chậm)
    public float finalSpawnRate = 0.25f;  // Lúc cuối: 0.25 giây/con (Ra cực nhanh)
    
    [Header("Danh sách 8 miệng ống")]
    public Transform[] spawnPoints; 

    private float nextSpawnTime;
    private bool hasStartedTimer = false; 
    private GameManager gm;
    private float totalStartTime;

    void Start()
    {
        gm = Object.FindAnyObjectByType<GameManager>();
    }

    void Update()
    {
        float currentSpawnRate = initialSpawnRate;

        // TÍNH TOÁN TỐC ĐỘ SINH QUÁI
        if (gm != null && gm.timerIsRunning)
        {
            if (!hasStartedTimer) {
                totalStartTime = gm.timeRemaining; // Lấy mốc 300s (5 phút)
                hasStartedTimer = true;
            }

            // Tỷ lệ thời gian: 0 (bắt đầu) -> 1 (hết giờ)
            float timeRatio = 1f - (gm.timeRemaining / totalStartTime); 
            timeRatio = Mathf.Clamp01(timeRatio);

            // SpawnRate giảm dần (nghĩa là quái ra nhanh dần)
            currentSpawnRate = Mathf.Lerp(initialSpawnRate, finalSpawnRate, timeRatio);
        }

        if (canSpawn && Time.time >= nextSpawnTime)
        {
            SpawnOneEnemy();
            nextSpawnTime = Time.time + currentSpawnRate;
        }
    }

    void SpawnOneEnemy()
    {
        if (!canSpawn || spawnPoints.Length == 0 || enemyPrefab == null) return; 

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedPipe = spawnPoints[randomIndex];

        GameObject newEnemy = Instantiate(enemyPrefab, selectedPipe.position, Quaternion.identity);

        // KÍCH HOẠT TIMER NẾU CHƯA CHẠY
        if (gm != null && !gm.timerIsRunning) gm.StartTimer();
    }

    public void StopSpawning() { canSpawn = false; }
}