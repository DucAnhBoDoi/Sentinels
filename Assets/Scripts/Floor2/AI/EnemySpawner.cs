using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Cài đặt quái")]
    public GameObject enemyPrefab; 
    public bool canSpawn = true; 

    [Header("Thiết lập tốc độ sinh (Spawn Rate)")]
    public float rateEasy = 2.5f;   // Phút 7-3: Ra chậm
    public float rateHard = 0.5f;   // Phút 3-2: Ra nhanh
    public float rateInsane = 0.15f; // Phút 2-0: Ra cực nhanh (bão quái)
    
    [Header("Danh sách 8 miệng ống")]
    public Transform[] spawnPoints; 

    private float nextSpawnTime;
    private bool hasStartedTimer = false; 
    private Floor2Manager fm;
    private float totalStartTime;

    // Biến trạng thái để truyền cho quái
    private float currentDamageMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;

    void Start()
    {
        fm = Object.FindAnyObjectByType<Floor2Manager>();
    }

    void Update()
    {
        if (fm == null || !fm.timerIsRunning) return;

        if (!hasStartedTimer) {
            totalStartTime = fm.timeRemaining; // 420 giây
            hasStartedTimer = true;
        }

        float timeRemaining = fm.timeRemaining;
        float currentSpawnRate;

        // --- HỆ THỐNG PHÂN CẤP ĐỘ KHÓ THEO TỪNG PHÚT ---

        if (timeRemaining <= 120f) // PHÚT THỨ 2 -> 0: GIAI ĐOẠN SINH TỒN CUỐI
        {
            currentDamageMultiplier = 2.0f; // Dame to gấp đôi
            currentSpeedMultiplier = 1.5f;  // Bay rất nhanh
            currentSpawnRate = rateInsane;  // Spawn cực dồn dập
        }
        else if (timeRemaining <= 180f) // PHÚT THỨ 3 -> 2: GIAI ĐOẠN TĂNG TỐC
        {
            currentDamageMultiplier = 1.0f; 
            currentSpeedMultiplier = 1.35f; // Bay nhanh hơn giai đoạn trước
            currentSpawnRate = rateHard;    // Spawn nhanh
        }
        else if (timeRemaining <= 240f) // PHÚT THỨ 4 -> 3: BẮT ĐẦU TĂNG TỐC BAY
        {
            currentDamageMultiplier = 1.0f;
            currentSpeedMultiplier = 1.2f;  // Bay nhanh hơn một tí
            currentSpawnRate = rateEasy;    // Vẫn spawn từ từ
        }
        else // PHÚT 7 -> 4: GIAI ĐOẠN LÀM QUEN
        {
            currentDamageMultiplier = 1.0f;
            currentSpeedMultiplier = 1.0f;  // Tốc độ bình thường
            currentSpawnRate = rateEasy;    // Spawn chậm
        }

        // Thực hiện Spawn
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

        BoidEnemy boidScript = newEnemy.GetComponent<BoidEnemy>();
        if (boidScript != null)
        {
            boidScript.attackDamage *= currentDamageMultiplier;
            boidScript.speed *= currentSpeedMultiplier;
            
            // Đổi màu để cảnh báo người chơi ở phút cuối (Phút thứ 2)
            if (currentDamageMultiplier > 1.5f) {
                SpriteRenderer sr = newEnemy.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.red; 
            }
            else if (currentSpeedMultiplier > 1.1f) {
                // Phút thứ 4 trở đi quái hơi cam để báo hiệu bay nhanh
                SpriteRenderer sr = newEnemy.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 0.7f, 0.4f);
            }
        }

        if (fm != null && !fm.timerIsRunning) fm.StartTimer();
    }

    public void StopSpawning() { canSpawn = false; }
}