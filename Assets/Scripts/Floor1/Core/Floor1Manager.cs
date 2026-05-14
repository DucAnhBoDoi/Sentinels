using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode; // BẮT BUỘC THÊM ĐỂ CHẠY MẠNG

// 1. ĐỔI TỪ MonoBehaviour SANG NetworkBehaviour
public class Floor1Manager : NetworkBehaviour
{
    public static Floor1Manager Instance;

    [Header("Tham chiếu Chuyển Màn")]
    public Transform elevatorDoor;
    public float interactDistance = 3f;

    [Header("Tham chiếu Người chơi")]
    public Transform playerA;
    public Transform playerB;

    [Header("Hiệu ứng Chuyển cảnh")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    // 2. BIẾN isLevelComplete ĐƯỢC NÂNG CẤP THÀNH BIẾN MẠNG
    private NetworkVariable<bool> isLevelComplete = new NetworkVariable<bool>(false);
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 3. THAY Start BẰNG OnNetworkSpawn ĐỂ ĐẢM BẢO MẠNG ĐÃ KẾT NỐI
    public override void OnNetworkSpawn()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        if (IsServer)
        {
            GameProgress.KeysCollected = 0; // Đặt lại số Key về 0 khi bắt đầu ván mới
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    // 4. HÀM MỚI CHO POWERGRIDMANAGER GỌI: BÁO CHO SERVER BIẾT ĐÃ XONG MÀN
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LevelCompleteServerRpc()
    {
        isLevelComplete.Value = true;
        // SỬA CHỮ [2] THÀNH [ENTER] TRONG LOG
        Debug.Log("<color=green>ĐIỆN ĐÃ CÓ! CẢ 2 NGƯỜI CHƠI HÃY LẠI GẦN CỬA VÀ BẤM PHÍM [ENTER] ĐỂ QUA TẦNG!</color>");
    }

    // ========================================================
    // CÁC HÀM GỌI TỪ BÊN NGOÀI (GAMEOVERMANAGER) VÀO
    // ========================================================
    public void QuitToMenuWithFade(string menuSceneName)
    {
        QuitToMenuServerRpc(menuSceneName);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void QuitToMenuServerRpc(string menuSceneName)
    {
        QuitToMenuClientRpc(menuSceneName);
    }

    [ClientRpc]
    private void QuitToMenuClientRpc(string menuSceneName)
    {
        StartCoroutine(TransitionToMenuSequence(menuSceneName));
    }


    public void RestartLevelWithFade()
    {
        RestartLevelServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RestartLevelServerRpc()
    {
        RestartLevelClientRpc();
    }

    [ClientRpc]
    private void RestartLevelClientRpc()
    {
        StartCoroutine(TransitionToRestartSequence());
    }

    // ========================================================
    // LOGIC CHECK KHOẢNG CÁCH (CHỈ SERVER LÀM ĐỂ TRÁNH LOẠN)
    // ========================================================
    void Update()
    {
        if (!IsServer || !isLevelComplete.Value || isTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // --- ĐÃ ĐỔI TỪ digit2Key SANG enterKey ---
        if (keyboard.enterKey.wasPressedThisFrame)
        {
            if (elevatorDoor == null) return;

            float distA = playerA ? Vector2.Distance(playerA.position, elevatorDoor.position) : float.MaxValue;
            float distB = playerB ? Vector2.Distance(playerB.position, elevatorDoor.position) : float.MaxValue;

            if (distA <= interactDistance && distB <= interactDistance)
            {
                Debug.Log("Cả 2 đã ở cửa! Đang tải Tầng 2...");
                StartNextFloorSequenceClientRpc();
            }
            else
            {
                Debug.Log("CẢ 2 NGƯỜI CHƠI phải đứng sát vào Cửa Thang Máy mới đi được!");
            }
        }
    }

    [ClientRpc]
    private void StartNextFloorSequenceClientRpc()
    {
        StartCoroutine(TransitionToNextFloor());
    }

    // ========================================================
    // CÁC COROUTINE CŨ CỦA ANH (GIỮ NGUYÊN 100% LOGIC)
    // ========================================================
    
    // 1. Tối dần đi rồi chuyển Scene
    IEnumerator TransitionToNextFloor()
    {
        isTransitioning = true;

        // --- TÌM NHẠC NỀN ĐỂ CHUẨN BỊ LÀM NHỎ ---
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
            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;

                // --- ÉP NHẠC NỀN NHỎ DẦN THEO TỐC ĐỘ TỐI CỦA MÀN HÌNH ---
                if (bgmSource != null)
                {
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                }

                yield return null;
            }
        }
        
        // Đảm bảo nhạc tắt hẳn khi màn hình đã đen xì
        if (bgmSource != null) bgmSource.volume = 0f;

        // QUAN TRỌNG: Chỉ Server mới gọi lệnh LoadScene mạng
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("GamePlayFloor2", LoadSceneMode.Single);
        }
    }

    // 2. Sáng dần lên lúc mới mở game
    IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            fadeImage.color = c;
            yield return null;
        }
        fadeImage.gameObject.SetActive(false);
    }

    IEnumerator TransitionToMenuSequence(string sceneName)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        Time.timeScale = 1f;
        QuestPopupManager.hasAcceptedOnce = false;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(sceneName);
    }

    IEnumerator TransitionToRestartSequence()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        Time.timeScale = 1f;

        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }
}