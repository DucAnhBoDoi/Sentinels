using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class Floor2Manager : NetworkBehaviour
{
    public static Floor2Manager Instance;

    [Header("Luật chơi Tầng 2 (Thời gian)")]
    // Tạo một biến bình thường để chỉnh trong Inspector
    public float initialTime = 300f;

    // Giấu biến mạng đi để tránh lỗi Editor
    [HideInInspector]
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(0f);

    [HideInInspector]
    public NetworkVariable<bool> timerIsRunning = new NetworkVariable<bool>(false);

    private NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isLevelComplete = new NetworkVariable<bool>(false);

    [Header("Giao diện UI")]
    public TextMeshProUGUI timeText;
    public GameObject coreHealthBar;

    [Header("Phần thưởng & Lõi")]
    public GameObject shardPrefab;
    public Transform coreTransform;
    public Vector3 shardOffset = new Vector3(0, -3f, 0);

    // --- THÊM ĐẠO CỤ CUTSCENE VÀO ĐÂY ---
    [Header("Cutscene Phim Trường")]
    public UnityEngine.Playables.PlayableDirector winDirector; 
    public GameObject fakeKeyVisual; 
    public GameObject waypointIcon; 
    
    [Tooltip("Thời gian chờ bảng Quest Complete tắt đi (giây)")]
    public float delayBeforeCutscene = 3f; // CHỜ 3 GIÂY ĐỂ BẢNG COMPLETE CHẠY XONG
    
    [HideInInspector] 
    public bool isCutscenePlaying = false; // BIẾN NÀY ĐỂ BẠN KHÓA DI CHUYỂN BÊN SCRIPT PLAYER

    [Header("Tham chiếu Chuyển Màn")]
    public Transform elevatorDoor;
    public float interactDistance = 3f;
    public Transform playerA;
    public Transform playerB;

    [Header("Hiệu ứng Chuyển cảnh")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    private bool isTransitioning = false;
    private bool hasDroppedShard = false;
    
    // BIẾN LƯU TRỮ NHẠC NỀN
    private AudioSource bgmSource;
    
    // THÊM BIẾN NÀY ĐỂ NHỚ ÂM LƯỢNG GỐC TRƯỚC KHI FADE-OUT
    private float initialBgmVolume = -1f;

    void Awake() { if (Instance == null) Instance = this; }

    public override void OnNetworkSpawn()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        if (IsServer)
        {
            timeRemaining.Value = initialTime;
            timerIsRunning.Value = false;
            isGameOver.Value = false;
            isLevelComplete.Value = false;
        }

        Time.timeScale = 1f;

        if (timeText != null) timeText.gameObject.SetActive(false);

        // ĐẢM BẢO ĐẠO CỤ CUTSCENE BỊ TẮT LÚC MỚI VÀO GAME
        if (fakeKeyVisual != null) fakeKeyVisual.SetActive(false);
        if (waypointIcon != null) waypointIcon.SetActive(false);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }

        // Client luôn lắng nghe thời gian từ Host để hiển thị
        timeRemaining.OnValueChanged += (prev, current) => DisplayTime(current);
    }

    [Rpc(SendTo.Server)]
    public void StartTimerServerRpc()
    {
        if (!timerIsRunning.Value && !isGameOver.Value)
        {
            timerIsRunning.Value = true;
            ShowTimerUIClientRpc();
        }
    }

    [ClientRpc]
    void ShowTimerUIClientRpc()
    {
        if (timeText != null) timeText.gameObject.SetActive(true);
    }

    void Update()
    {
        // 1. CHẠY THỜI GIAN TRÊN SERVER
        if (IsServer && timerIsRunning.Value && !isGameOver.Value)
        {
            if (timeRemaining.Value > 0)
            {
                timeRemaining.Value -= Time.deltaTime;
            }
            else
            {
                timeRemaining.Value = 0;
                timerIsRunning.Value = false;
                isGameOver.Value = true;
                WinGameClientRpc(); // Hết giờ -> Lõi còn sống -> Thắng

                if (QuestUIManager.Instance != null)
                {
                    QuestUIManager.Instance.TriggerQuestCompleteNetwork();
                }
            }
        }

        // --- LOGIC MỚI: TẤT CẢ CÁC MÁY CÙNG KIỂM TRA ĐỂ LÀM NHỎ NHẠC TRONG 5 GIÂY CUỐI ---
        if (timerIsRunning.Value && !isGameOver.Value)
        {
            if (timeRemaining.Value <= 5f && timeRemaining.Value > 0f)
            {
                if (bgmSource == null)
                {
                    GameObject bgmManager = GameObject.Find("BGM_Manager");
                    if (bgmManager != null) bgmSource = bgmManager.GetComponent<AudioSource>();
                }

                if (bgmSource != null)
                {
                    // Ghi nhớ âm lượng hiện tại (vd: 0.135) đúng một lần duy nhất lúc bắt đầu 5s cuối
                    if (initialBgmVolume < 0f)
                    {
                        initialBgmVolume = bgmSource.volume;
                    }

                    // Lấy âm lượng gốc nhân với tỷ lệ thời gian để nhỏ dần đều
                    bgmSource.volume = initialBgmVolume * (timeRemaining.Value / 5f);
                }
            }
        }
        
        // Đảm bảo nhạc tắt hẳn khi hết giờ (hoặc thắng/thua)
        if (!timerIsRunning.Value && bgmSource != null && bgmSource.volume > 0)
        {
            bgmSource.volume = 0f;
        }

        // 2. SERVER CHECK ĐIỀU KIỆN QUA MÀN
        if (!IsServer || !isLevelComplete.Value || isTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (elevatorDoor == null) return;

            float distA = playerA ? Vector2.Distance(playerA.position, elevatorDoor.position) : float.MaxValue;
            float distB = playerB ? Vector2.Distance(playerB.position, elevatorDoor.position) : float.MaxValue;

            if (distA <= interactDistance && distB <= interactDistance)
            {
                Debug.Log("Cả 2 đã ở cửa! Đang tải Tầng 3...");
                StartNextFloorSequenceClientRpc();
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TriggerGameOverServerRpc()
    {
        if (isGameOver.Value) return;
        isGameOver.Value = true;
        timerIsRunning.Value = false;

        // Gọi ClientRpc để tất cả các máy cùng hiện bảng thua cuộc
        ShowGameOverClientRpc();
    }

    [ClientRpc]
    void ShowGameOverClientRpc()
    {
        // 1. Giấu các UI không cần thiết đi
        HideUIClientRpc();

        // 2. Hiện cái bảng Game Over lên (Dùng chung GameOverManager mà bạn đã tạo)
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }

    [ClientRpc]
    void HideUIClientRpc()
    {
        if (timeText != null) timeText.gameObject.SetActive(false);
        if (coreHealthBar != null) coreHealthBar.SetActive(false);
    }

    [ClientRpc]
    void WinGameClientRpc()
    {
        HideUIClientRpc();

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
            foreach (var s in scripts)
            {
                if (s != this) s.enabled = false;
            }
            Destroy(enemy, 1.5f);
        }

        // --- GỌI COROUTINE CHẠY CUTSCENE RỒI MỚI ĐẺ ITEM THẬT ---
        StartCoroutine(PlayWinCutsceneRoutine());
    }

    private IEnumerator PlayWinCutsceneRoutine()
    {
        // 0. CHỜ BẢNG QUEST COMPLETE HIỆN XONG (3 GIÂY) MỚI CHẠY PHIM
        yield return new WaitForSeconds(delayBeforeCutscene);

        // --- BẮT ĐẦU CHIẾU PHIM: KHÓA DI CHUYỂN NGƯỜI CHƠI ---
        isCutscenePlaying = true;
        if (playerA != null) playerA.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        if (playerB != null) playerB.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        // 1. Chạy phim (Cả Server và Client đều chạy)
        if (winDirector != null)
        {
            winDirector.Play();
            // Đợi phim chạy xong
            yield return new WaitForSeconds((float)winDirector.duration); 
        }

        // 2. Phim kết thúc: Giấu cục Key diễn viên đóng thế đi
        if (fakeKeyVisual != null) fakeKeyVisual.SetActive(false);

        // 3. CHỈ SERVER ĐƯỢC ĐẺ VẬT PHẨM (SHARD) MẠNG ĐỂ ĐỒNG BỘ
        if (IsServer && shardPrefab != null && coreTransform != null && !hasDroppedShard)
        {
            hasDroppedShard = true;
            Vector3 spawnPosition = coreTransform.position + shardOffset;
            GameObject spawnedShard = Instantiate(shardPrefab, spawnPosition, Quaternion.identity);
            spawnedShard.GetComponent<NetworkObject>().Spawn(); // Khai sinh mạng cho cục Shard
        }

        // 4. HIỆN WAYPOINTS LÊN SAU KHI PHIM ĐÃ CHIẾU XONG XUÔI VÀ ITEM ĐÃ ĐẺ RA
        if (waypointIcon != null) waypointIcon.SetActive(true);

        // --- PHIM XONG, MỞ KHÓA DI CHUYỂN CHO NGƯỜI CHƠI ĐI NHẶT ĐỒ ---
        isCutscenePlaying = false;
    }

    public void LevelComplete()
    {
        if (IsServer) isLevelComplete.Value = true;
    }

    // --- CÁC HÀM HIỆU ỨNG VÀ CHUYỂN MÀN (GIỮ NGUYÊN GỐC CỦA BẠN) ---
    [ClientRpc]
    private void StartNextFloorSequenceClientRpc() { StartCoroutine(TransitionToNextFloor()); }

    IEnumerator TransitionToNextFloor()
    {
        isTransitioning = true;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) 
            { 
                elapsed += Time.deltaTime; 
                c.a = Mathf.Clamp01(elapsed / fadeDuration); 
                fadeImage.color = c; 
                yield return null; 
            }
        }

        if (IsServer) NetworkManager.Singleton.SceneManager.LoadScene("GamePlayFloor3", LoadSceneMode.Single);
    }

    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;
        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color; c.a = 1f; fadeImage.color = c;
        yield return new WaitForSeconds(0.5f);
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime; c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration)); fadeImage.color = c; yield return null;
        }
        fadeImage.gameObject.SetActive(false);
    }

    // HỆ THỐNG RESTART & QUIT Y HỆT GỐC CỦA BẠN
    public void RestartLevelWithFade() { RestartLevelServerRpc(); }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] private void RestartLevelServerRpc() { RestartLevelClientRpc(); }
    [ClientRpc] private void RestartLevelClientRpc() { StartCoroutine(TransitionToRestartSequence()); }

    public void QuitToMenuWithFade(string menuSceneName) { QuitToMenuServerRpc(menuSceneName); }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] private void QuitToMenuServerRpc(string menuSceneName) { QuitToMenuClientRpc(menuSceneName); }
    [ClientRpc] private void QuitToMenuClientRpc(string menuSceneName) { StartCoroutine(TransitionToMenuSequence(menuSceneName)); }

    IEnumerator TransitionToRestartSequence()
    {
        isTransitioning = true;
        Time.timeScale = 0f; // Khóa chặt thời gian ngay lập tức để quái vật không nhúc nhích được
        
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        
        Time.timeScale = 1f; // Trả lại thời gian trước khi Load scene mới
        if (IsServer) NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    IEnumerator TransitionToMenuSequence(string sceneName)
    {
        isTransitioning = true;
        Time.timeScale = 0f; // Khóa chặt thời gian

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        
        Time.timeScale = 1f; 
        QuestPopupManager.ResetQuestState(); // Đây là hàm gốc có sẵn của bạn
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(sceneName);
    }
}