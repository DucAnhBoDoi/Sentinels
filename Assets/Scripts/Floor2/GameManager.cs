using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    [Header("Cấu hình thời gian")]
    public float timeRemaining = 300f;
    public bool timerIsRunning = false; 
    private bool isGameOver = false; // Biến khóa trạng thái kết thúc

    [Header("Giao diện UI")]
    public TextMeshProUGUI timeText; 
    public GameObject winPanel;      

    [Header("Phần thưởng chiến thắng")]
    public GameObject shardPrefab;   
    public Transform coreTransform;  
    public Vector3 shardOffset = new Vector3(0, -2.5f, 0); 

    void Start()
    {
        Time.timeScale = 1f; 
        isGameOver = false;
        if (winPanel != null) winPanel.SetActive(false);
        DisplayTime(timeRemaining); 

        if (timeText != null) 
        {
            timeText.gameObject.SetActive(false); 
        }
    }

    public void StartTimer()
    {
        if (!timerIsRunning && !isGameOver)
        {
            timerIsRunning = true;
            if (timeText != null) 
            {
                timeText.gameObject.SetActive(true); 
            }
        }
    }

    void Update()
    {
        if (isGameOver) return; // Nếu đã thắng, ngừng mọi tính toán thời gian

        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                DisplayTime(0); 
                timerIsRunning = false;
                isGameOver = true; // Kích hoạt trạng thái kết thúc
                WinGame();
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeText == null) return;
        float minutes = Mathf.FloorToInt(Mathf.Max(0, timeToDisplay) / 60); 
        float seconds = Mathf.FloorToInt(Mathf.Max(0, timeToDisplay) % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void WinGame()
    {
        if (winPanel != null) winPanel.SetActive(true);

        // 1. DỪNG SINH QUÁI MỚI NGAY LẬP TỨC
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning(); // Đảm bảo EnemySpawner đã có hàm này
        }

        // 2. XÓA SẠCH QUÁI TRÊN TOÀN BẢN ĐỒ
        // Lưu ý: Bạn PHẢI gán Tag "Enemy" cho Prefab quái như đã làm ở ảnh image_b27116.png
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        // 3. XUẤT HIỆN MẢNH VỠ
        if (shardPrefab != null && coreTransform != null)
        {
            GameObject spawnedShard = Instantiate(shardPrefab, coreTransform.position + shardOffset, Quaternion.identity);
            
            SpriteRenderer sr = spawnedShard.GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.sortingLayerName = "Player"; 
                sr.sortingOrder = 10;
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}