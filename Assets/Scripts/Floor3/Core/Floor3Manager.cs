using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG
using Scripts.Floor3.Core;

// ĐỔI SANG NetworkBehaviour
public class Floor3Manager : NetworkBehaviour
{
    public static Floor3Manager Instance;

    [Header("Tham chiếu Chuyển Màn Tầng 4")]
    public Transform elevatorDoor;
    public float interactDistance = 3f;

    [Header("Tham chiếu Người chơi")]
    public Transform playerA;
    public Transform playerB;

    [Header("Hiệu ứng Chuyển cảnh")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    // --- SỬA LẠI: GIỐNG Y HỆT TẦNG 2 ---
    [Header("Phần thưởng Tầng 3")]
    public GameObject shardPrefab;
    public Transform targetTransform; // Bạn kéo con Robot hoặc Điểm đến của nó vào đây
    public Vector3 shardOffset = new Vector3(0, -3f, 0);

    [Header("Cutscene Phim Trường")]
    public UnityEngine.Playables.PlayableDirector winDirector;
    public GameObject fakeKeyVisual;
    public GameObject waypointIcon;
    public float delayBeforeCutscene = 3f;
    [HideInInspector] public bool isCutscenePlaying = false;

    // --- THÊM BIẾN NÀY ĐỂ NẮM ĐẦU THẰNG MẸ RobotHUD ---
    [Header("Giao diện Robot")]
    public GameObject robotHUD;

    // Biến mạng đồng bộ điểm số đúng
    [HideInInspector] public NetworkVariable<int> correctAnswersCount = new NetworkVariable<int>(0);

    // Biến mạng đồng bộ trạng thái hoàn thành màn chơi
    private NetworkVariable<bool> isLevelComplete = new NetworkVariable<bool>(false);
    private bool isTransitioning = false;
    private bool hasDroppedShard = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // --- KHI ROBOT TỚI ĐÍCH THÌ GỌI HÀM NÀY ---
    void OnEnable()  { GameContext.OnLevelComplete += RobotReachedDestination; }
    void OnDisable() { GameContext.OnLevelComplete -= RobotReachedDestination; }

    public override void OnNetworkSpawn()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        if (IsServer)
        {
            isLevelComplete.Value = false;
            correctAnswersCount.Value = 0; // Reset điểm về 0
        }

        if (fakeKeyVisual != null) fakeKeyVisual.SetActive(false);
        if (waypointIcon != null) waypointIcon.SetActive(false);

        if (robotHUD != null) robotHUD.SetActive(true);

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    [Rpc(SendTo.Server)]
    public void AddCorrectScoreServerRpc()
    {
        correctAnswersCount.Value++;
        Debug.Log($"[Floor3Manager] Điểm hiện tại: {correctAnswersCount.Value}/5");
    }

    // ========================================================
    // BƯỚC 1: ROBOT ĐẾN ĐÍCH -> KIỂM TRA ĐIỂM -> CHIẾU PHIM -> RỚT KEY
    // ========================================================
    public void RobotReachedDestination()
    {
        if (IsServer)
        {
            if (QuestUIManager.Instance != null)
            {
                QuestUIManager.Instance.TriggerQuestCompleteNetwork();
            }

            // Kiểm tra điểm: Lớn hơn hoặc bằng 3 thì có Cutscene, ngược lại thì không
            bool isEligibleForCutscene = correctAnswersCount.Value >= 3;
            
            // Gọi ClientRpc để tất cả các máy cùng chạy chuỗi hoàn thành
            ExecuteWinSequenceClientRpc(isEligibleForCutscene);
        }
    }

    [ClientRpc]
    private void ExecuteWinSequenceClientRpc(bool runCutscene)
    {
        StartCoroutine(WinSequenceRoutine(runCutscene));
    }

    private IEnumerator WinSequenceRoutine(bool runCutscene)
    {
        // NGAY KHI ĐẾN ĐÍCH, GIẤU THANH MÁU & ICON CỦA ROBOT ĐI
        if (robotHUD != null) robotHUD.SetActive(false);

        // CHỜ 3 GIÂY CHO BẢNG QUEST COMPLETE CHẠY XONG
        yield return new WaitForSeconds(delayBeforeCutscene);

        // XỬ LÝ PHÂN NHÁNH: CÓ CHIẾU PHIM HAY KHÔNG?
        if (runCutscene)
        {
            // BẮT ĐẦU CHIẾU PHIM: KHÓA DI CHUYỂN
            isCutscenePlaying = true;
            if (playerA != null) playerA.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            if (playerB != null) playerB.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

            if (waypointIcon != null) waypointIcon.SetActive(false);

            // CHIẾU PHIM
            if (winDirector != null)
            {
                winDirector.Play();
                yield return new WaitForSeconds((float)winDirector.duration); 
            }

            // GIẤU CHÌA KHÓA GIẢ ĐI
            if (fakeKeyVisual != null) fakeKeyVisual.SetActive(false);

            // ĐẺ CHÌA KHÓA THẬT VỚI OFFSET (CHỈ SERVER LÀM)
            if (IsServer && shardPrefab != null && targetTransform != null && !hasDroppedShard)
            {
                hasDroppedShard = true;
                Vector3 spawnPosition = targetTransform.position + shardOffset;
                GameObject spawnedShard = Instantiate(shardPrefab, spawnPosition, Quaternion.identity);
                spawnedShard.GetComponent<NetworkObject>().Spawn(); 
            }

            // MỞ KHÓA DI CHUYỂN VÀ BẬT LẠI WAYPOINT
            if (waypointIcon != null) waypointIcon.SetActive(true);
            isCutscenePlaying = false;
        }
        else
        {
            // NẾU KHÔNG ĐỦ ĐIỂM -> KHÔNG CÓ CUTSCENE, VẪN CHỈ ĐƯỜNG ĐI TIẾP
            if (waypointIcon != null) waypointIcon.SetActive(true);

            // --- ĐÂY LÀ CHỖ VÁ LỖI CỰC KỲ QUAN TRỌNG ĐỂ KHÔNG BỊ KẸT GAME ---
            if (IsServer)
            {
                isLevelComplete.Value = true; // Cho phép qua màn luôn vì không có chìa khóa để nhặt
                Debug.Log("<color=yellow>NHIỆM VỤ HOÀN THÀNH NHƯNG KHÔNG ĐỦ ĐIỂM NHẬN LÕI! BẤM [ENTER] Ở CỬA ĐỂ QUA TẦNG 4!</color>");
            }
        }
    }

    // ========================================================
    // BƯỚC 2: NGƯỜI CHƠI NHẶT ĐƯỢC KEY -> MỞ KHÓA CỬA THANG MÁY
    // (Hàm này được gọi từ file ShardCollector.cs khi bấm F)
    // ========================================================
    public void LevelComplete()
    {
        if (IsServer)
        {
            isLevelComplete.Value = true; // Chính thức cho phép qua màn
            Debug.Log("<color=green>ĐÃ NHẶT LÕI NĂNG LƯỢNG! BẤM PHÍM [ENTER] Ở CỬA ĐỂ QUA TẦNG 4!</color>");
        }
    }

    // ========================================================

    void Update()
    {
        // Phải nhặt được Key (hoặc tự qua màn nếu trả lời sai) thì mới được vô cửa
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
                Debug.Log("Cả 2 đã ở cửa! Đang tải Tầng 4...");
                StartNextFloorSequenceClientRpc();
            }
        }
    }

    [ClientRpc]
    private void StartNextFloorSequenceClientRpc()
    {
        StartCoroutine(TransitionToNextFloor());
    }

    IEnumerator TransitionToNextFloor()
    {
        isTransitioning = true;

        AudioSource bgmSource = null;
        float startVol = 0f;
        GameObject bgmManager = GameObject.Find("BGM_Manager");
        if (bgmManager != null)
        {
            bgmSource = bgmManager.GetComponent<AudioSource>();
            if (bgmSource != null) startVol = bgmSource.volume;
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) 
            { 
                elapsed += Time.deltaTime; 
                c.a = Mathf.Clamp01(elapsed / fadeDuration); 
                fadeImage.color = c; 

                if (bgmSource != null)
                {
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                }

                yield return null; 
            }
        }
        
        if (bgmSource != null) bgmSource.volume = 0f;

        if (IsServer) NetworkManager.Singleton.SceneManager.LoadScene("GamePlayFloor4", LoadSceneMode.Single);
    }

    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f; Color c = fadeImage.color;
        c.a = 1f; fadeImage.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            fadeImage.color = c;
            yield return null;
        }
        fadeImage.gameObject.SetActive(false);
    }

    // --- HỆ THỐNG RESTART / QUIT ĐỒNG BỘ MẠNG ---
    public void RestartLevelWithFade() { RestartLevelServerRpc(); }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] private void RestartLevelServerRpc() { RestartLevelClientRpc(); }
    [ClientRpc] private void RestartLevelClientRpc() { StartCoroutine(TransitionToRestartSequence()); }

    public void QuitToMenuWithFade(string menuSceneName) { QuitToMenuServerRpc(menuSceneName); }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] private void QuitToMenuServerRpc(string menuSceneName) { QuitToMenuClientRpc(menuSceneName); }
    [ClientRpc] private void QuitToMenuClientRpc(string menuSceneName) { StartCoroutine(TransitionToMenuSequence(menuSceneName)); }

    IEnumerator TransitionToRestartSequence()
    {
        isTransitioning = true;
        Time.timeScale = 0f; 
        
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        
        Time.timeScale = 1f;
        if (IsServer) NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    IEnumerator TransitionToMenuSequence(string sceneName)
    {
        isTransitioning = true;
        Time.timeScale = 0f;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true); float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        
        Time.timeScale = 1f; 
        QuestPopupManager.hasAcceptedOnce = false;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(sceneName);
    }
}