using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode; // BẮT BUỘC THÊM ĐỂ CHẠY MẠNG

// ĐỔI TỪ MonoBehaviour SANG NetworkBehaviour
public class Floor4Manager : NetworkBehaviour
{
    public static Floor4Manager Instance;

    // ĐÃ XÓA THAM CHIẾU CỬA THANG MÁY (KHÔNG CẦN NỮA VÌ ĐÂY LÀ PHÒNG BOSS)

    [Header("Tham chiếu Người chơi")]
    public Transform playerA;
    public Transform playerB;

    [Header("Hiệu ứng Chuyển cảnh")]
    public Image fadeImage;
    public float fadeDuration = 1.5f;

    // BIẾN isLevelComplete ĐƯỢC NÂNG CẤP THÀNH BIẾN MẠNG
    private NetworkVariable<bool> isLevelComplete = new NetworkVariable<bool>(false);
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // THAY Start BẰNG OnNetworkSpawn ĐỂ ĐẢM BẢO MẠNG ĐÃ KẾT NỐI
    public override void OnNetworkSpawn()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    // HÀM BÁO CHO SERVER BIẾT ĐÃ XONG MÀN (Gọi khi Boss chết)
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LevelCompleteServerRpc()
    {
        isLevelComplete.Value = true;
        // ĐỔI LOG ĐỂ NGƯỜI CHƠI BIẾT CHỈ CẦN BẤM ENTER LÀ XONG
        Debug.Log("<color=green>ĐÃ TIÊU DIỆT BOSS! BẤM PHÍM [ENTER] ĐỂ KẾT THÚC GAME!</color>");
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
    // LOGIC CHECK QUA MÀN TẦNG 4 (KHÔNG CẦN KIỂM TRA CỬA)
    // ========================================================
    void Update()
    {
        if (!IsServer || !isLevelComplete.Value || isTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Chỉ cần bấm Enter là kết thúc game, không bị khóa bởi khoảng cách nữa
        if (keyboard.enterKey.wasPressedThisFrame)
        {
            Debug.Log("Đang tải cảnh chiến thắng/Menu...");
            StartNextFloorSequenceClientRpc();
        }
    }

    [ClientRpc]
    private void StartNextFloorSequenceClientRpc()
    {
        StartCoroutine(TransitionToNextFloor());
    }

    // ========================================================
    // CÁC COROUTINE CŨ CỦA BẠN (GIỮ NGUYÊN 100% LOGIC)
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
            // TẦNG 4 LÀ TẦNG CUỐI, NÊN SẼ TRẢ VỀ MENU SCENE
            NetworkManager.Singleton.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
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