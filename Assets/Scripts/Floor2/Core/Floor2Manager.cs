using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class Floor2Manager : MonoBehaviour
{
    public static Floor2Manager Instance;

    [Header("Luật chơi Tầng 2 (Thời gian)")]
    public float timeRemaining = 300f;
    public bool timerIsRunning = false;
    private bool isGameOver = false;

    [Header("Giao diện UI")] // GOM CHUNG UI VÀO ĐÂY
    public TextMeshProUGUI timeText;
    public GameObject coreHealthBar; // BỔ SUNG BIẾN NÀY ĐỂ GIẤU THANH MÁU

    [Header("Phần thưởng & Lõi")]
    public GameObject shardPrefab;
    public Transform coreTransform;
    public Vector3 shardOffset = new Vector3(0, -3f, 0);

    [Header("Tham chiếu Chuyển Màn")]
    public Transform elevatorDoor;
    public float interactDistance = 3f;
    public Transform playerA;
    public Transform playerB;

    [Header("Hiệu ứng Chuyển cảnh")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    private bool isLevelComplete = false;
    private bool isTransitioning = false;
    private bool hasDroppedShard = false;

    void Awake() { if (Instance == null) Instance = this; }

    void Start()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        Time.timeScale = 1f;
        isGameOver = false;
        DisplayTime(timeRemaining);

        if (timeText != null) timeText.gameObject.SetActive(false);

        // Sáng dần khi vừa load Tầng 2
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    public void StartTimer()
    {
        if (!timerIsRunning && !isGameOver)
        {
            timerIsRunning = true;
            if (timeText != null) timeText.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        // Kiểm tra xem Lõi còn sống không
        LifeCore core = Object.FindAnyObjectByType<LifeCore>();
        if (core != null && core.energy <= 0 && !isGameOver)
        {
            isGameOver = true;
            timerIsRunning = false;
            
            // Ẩn luôn UI nếu thua cho gọn màn hình (Tuỳ chọn)
            if (timeText != null) timeText.gameObject.SetActive(false);
            if (coreHealthBar != null) coreHealthBar.SetActive(false);
            
            return; 
        }

        // 1. CHẠY THỜI GIAN
        if (timerIsRunning && !isGameOver)
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
                WinGame(); // Hết giờ -> Lõi còn sống -> Thắng
            }
        }

        // 2. LOGIC TỚI CỬA QUA TẦNG 3
        if (!isLevelComplete || isTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Bấm phím 3 để qua Tầng 3
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            if (elevatorDoor == null) return;

            float distA = playerA ? Vector2.Distance(playerA.position, elevatorDoor.position) : float.MaxValue;
            float distB = playerB ? Vector2.Distance(playerB.position, elevatorDoor.position) : float.MaxValue;

            if (distA <= interactDistance && distB <= interactDistance)
            {
                Debug.Log("Cả 2 đã ở cửa! Đang tải Tầng 3...");
                StartCoroutine(TransitionToNextFloor());
            }
            else
            {
                Debug.Log("CẢ 2 NGƯỜI CHƠI phải đứng sát vào Cửa Thang Máy!");
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
        // === GIẤU UI KHI CHIẾN THẮNG ===
        if (timeText != null) timeText.gameObject.SetActive(false);
        if (coreHealthBar != null) coreHealthBar.SetActive(false);

        // Dừng sinh quái
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        if (spawner != null) spawner.StopSpawning();

        // Ép quái chết từ từ có Animation
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("isDead");

            Collider2D col = enemy.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
            foreach (var s in scripts) {
                if (s != this) s.enabled = false;
            }

            Destroy(enemy, 1.5f);
        }

        // Rơi Shard
        if (shardPrefab != null && coreTransform != null && !hasDroppedShard)
        {
            hasDroppedShard = true;
            Vector3 spawnPosition = coreTransform.position + shardOffset;
            GameObject spawnedShard = Instantiate(shardPrefab, spawnPosition, Quaternion.identity);

            SpriteRenderer sr = spawnedShard.GetComponent<SpriteRenderer>();
            if (sr != null) { sr.sortingLayerName = "Player"; sr.sortingOrder = 10; }
        }
    }

    // Gọi hàm này khi nhặt được Shard
    public void LevelComplete()
    {
        isLevelComplete = true;
        Debug.Log("<color=green>ĐÃ LẤY ĐƯỢC LÕI! HÃY ĐẾN CỬA VÀ BẤM PHÍM [3] ĐỂ QUA TẦNG 3!</color>");
    }

    // --- CÁC HÀM HIỆU ỨNG CHUYỂN CẢNH ---
    IEnumerator TransitionToNextFloor()
    {
        isTransitioning = true;
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.deltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        SceneManager.LoadScene("GamePlayFloor3"); // Load Tầng 3
    }

    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        c.a = 1f; 
        fadeImage.color = c;

        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            fadeImage.color = c;
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
    }

    public void RestartLevelWithFade() { StartCoroutine(TransitionToRestartSequence()); }
    public void QuitToMenuWithFade(string menuSceneName) { StartCoroutine(TransitionToMenuSequence(menuSceneName)); }

    IEnumerator TransitionToRestartSequence()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator TransitionToMenuSequence(string sceneName)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        Time.timeScale = 1f;
        QuestPopupManager.hasAcceptedOnce = false; 
        SceneManager.LoadScene(sceneName);
    }
}