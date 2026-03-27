using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    [Header("Cấu hình thời gian")]
    public float timeRemaining = 300f;
    public bool timerIsRunning = false; 
    private bool isGameOver = false; 

    [Header("Giao diện UI")]
    public TextMeshProUGUI timeText; 
    public GameObject winPanel;      

    [Header("Phần thưởng chiến thắng")]
    public GameObject shardPrefab;   
    public Transform coreTransform;  
    [Tooltip("Chỉnh khoảng cách rơi ra xa Core. Ví dụ: Y = -3 để rơi xuống dưới.")]
    public Vector3 shardOffset = new Vector3(0, -3f, 0); 

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
        if (isGameOver) return; 

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
                isGameOver = true; 
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
        // 1. Hiển thị bảng thông báo thắng (nếu có)
        if (winPanel != null) winPanel.SetActive(true);

        // 2. Dừng sinh quái
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.StopSpawning(); 
        }

        // 3. Xóa sạch quái còn sót lại
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        // 4. XUẤT HIỆN MẢNH VỠ TẠI VỊ TRÍ OFFSET
        if (shardPrefab != null && coreTransform != null)
        {
            // Tính toán vị trí mới dựa trên offset bạn chỉnh
            Vector3 spawnPosition = coreTransform.position + shardOffset;
            
            GameObject spawnedShard = Instantiate(shardPrefab, spawnPosition, Quaternion.identity);
            
            // Đảm bảo mảnh vỡ nằm trên lớp Player để dễ thấy
            SpriteRenderer sr = spawnedShard.GetComponent<SpriteRenderer>();
            if (sr != null) {
                sr.sortingLayerName = "Player"; 
                sr.sortingOrder = 10;
            }
            
            Debug.Log("Shard đã rơi tại vị trí: " + spawnPosition);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}