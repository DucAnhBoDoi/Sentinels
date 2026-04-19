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
    
    // ĐỔI SANG Floor2Manager
    private Floor2Manager fm;
    private float totalStartTime;

    void Start()
    {
        // TÌM Floor2Manager THAY VÌ GameManager
        fm = Object.FindAnyObjectByType<Floor2Manager>();
    }

    void Update()
    {
        float currentSpawnRate = initialSpawnRate;

        // TÍNH TOÁN TỐC ĐỘ SINH QUÁI
        if (fm != null && fm.timerIsRunning)
        {
            if (!hasStartedTimer) {
                totalStartTime = fm.timeRemaining; // Lấy mốc 300s (5 phút)
                hasStartedTimer = true;
            }

            // Tỷ lệ thời gian: 0 (bắt đầu) -> 1 (hết giờ)
            float timeRatio = 1f - (fm.timeRemaining / totalStartTime); 
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

        Instantiate(enemyPrefab, selectedPipe.position, Quaternion.identity);

        // KÍCH HOẠT TIMER (Đã có RoomEventController kích hoạt nên dòng này có thể bỏ qua, nhưng giữ lại cho an toàn)
        if (fm != null && !fm.timerIsRunning) fm.StartTimer();
    }

    public void StopSpawning() { canSpawn = false; }
}