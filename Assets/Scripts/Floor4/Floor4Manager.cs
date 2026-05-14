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

    // --- THÊM ĐẠO CỤ CUTSCENE ENDING ---
    [Header("Cutscene Ending")]
    public UnityEngine.Playables.PlayableDirector badEndingDirector;
    public UnityEngine.Playables.PlayableDirector goodEndingDirector; // Chuẩn bị sẵn ô này cho Good End sau này

    // BIẾN isLevelComplete ĐƯỢC NÂNG CẤP THÀNH BIẾN MẠNG
    private NetworkVariable<bool> isLevelComplete = new NetworkVariable<bool>(false);

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
        if (isLevelComplete.Value) return; // Tránh gọi 2 lần
        isLevelComplete.Value = true;

        // KIỂM TRA SỐ LÕI THẬT: Nhặt đủ 2 lõi từ các tầng trước thì ra Good Ending
        bool isGoodEnding = (GameProgress.KeysCollected >= 2);

        // Gọi tất cả các máy cùng chạy Cutscene Ending tự động
        StartEndingSequenceClientRpc(isGoodEnding);
    }

    [ClientRpc]
    private void StartEndingSequenceClientRpc(bool isGoodEnding)
    {
        StartCoroutine(EndingSequenceRoutine(isGoodEnding));
    }

    // LUỒNG CUTSCENE: ĐÁNH BOSS XONG -> TỐI MÀN HÌNH -> CHIẾU PHIM -> TỐI MÀN HÌNH -> VỀ MENU
    private IEnumerator EndingSequenceRoutine(bool isGoodEnding)
    {
        // 1. TỐI DẦN MÀN HÌNH LÚC VỪA ĐÁNH BOSS XONG
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
                yield return null;
            }
        }

        // 2. BẬT CUTSCENE LÊN
        if (!isGoodEnding && badEndingDirector != null)
        {
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            badEndingDirector.Play();
            yield return new WaitForSeconds((float)badEndingDirector.duration);

            // --- THÊM DÒNG NÀY: Tắt UI Ending đi sau khi phim xong để lộ cái Fade_Image ở dưới ---
            if (badEndingDirector.gameObject.activeInHierarchy)
            {
                // Tìm đến cái BadEnding_UI trong Canvas và tắt nó đi
                GameObject badUI = GameObject.Find("BadEnding_UI");
                if (badUI != null) badUI.SetActive(false);
            }
        }
        else if (isGoodEnding && goodEndingDirector != null)
        {
            if (fadeImage != null) fadeImage.gameObject.SetActive(false);
            goodEndingDirector.Play();
            yield return new WaitForSeconds((float)goodEndingDirector.duration);

            // --- THÊM DÒNG NÀY: Tương tự cho Good Ending ---
            GameObject goodUI = GameObject.Find("GoodEnding_UI");
            if (goodUI != null) goodUI.SetActive(false);
        }

        // 3. CUTSCENE XONG -> TỐI DẦN MÀN HÌNH LẦN NỮA & TẮT NHẠC
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
            fadeImage.gameObject.SetActive(true); // Lúc này Fade_Image mới hiện lên và che được toàn bộ màn hình
            float elapsed = 0f;
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;

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

        // 4. CHUYỂN VỀ MENUSCENE
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }
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

    void Update()
    {
        // Trống trơn, vì giờ đây mọi thứ đã được chuyển sang chế độ tự động 100%!
    }

    // ========================================================
    // CÁC COROUTINE CŨ CỦA BẠN (GIỮ NGUYÊN 100% LOGIC)
    // ========================================================

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