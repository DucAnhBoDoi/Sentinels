using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class Floor1Manager : MonoBehaviour
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

    private bool isLevelComplete = false;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    public void LevelComplete()
    {
        isLevelComplete = true;
        Debug.Log("<color=green>ĐIỆN ĐÃ CÓ! CẢ 2 NGƯỜI CHƠI HÃY LẠI GẦN CỬA VÀ BẤM PHÍM [2] ĐỂ QUA TẦNG!</color>");
    }

    public void QuitToMenuWithFade(string menuSceneName)
    {
        StartCoroutine(TransitionToMenuSequence(menuSceneName));
    }

    public void RestartLevelWithFade()
    {
        StartCoroutine(TransitionToRestartSequence());
    }

    void Update()
    {
        if (!isLevelComplete || isTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            if (elevatorDoor == null) return;

            float distA = playerA ? Vector2.Distance(playerA.position, elevatorDoor.position) : float.MaxValue;
            float distB = playerB ? Vector2.Distance(playerB.position, elevatorDoor.position) : float.MaxValue;

            if (distA <= interactDistance && distB <= interactDistance)
            {
                Debug.Log("Cả 2 đã ở cửa! Đang tải Tầng 2...");
                StartCoroutine(TransitionToNextFloor());
            }
            else
            {
                Debug.Log("CẢ 2 NGƯỜI CHƠI phải đứng sát vào Cửa Thang Máy mới đi được!");
            }
        }
    }


    // 1. Tối dần đi rồi chuyển Scene
    IEnumerator TransitionToNextFloor()
    {
        isTransitioning = true;

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
        SceneManager.LoadScene("GamePlayFloor2");
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
        // Quan trọng: Phải dùng unscaledDeltaTime vì có thể game đang bị dừng (Time.timeScale = 0)
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                // Dùng unscaledDeltaTime để bỏ qua việc dừng thời gian
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        // Trả lại thời gian về 1 để Scene Menu chạy bình thường
        Time.timeScale = 1f;
        QuestPopupManager.hasAcceptedOnce = false;
        SceneManager.LoadScene(sceneName);
    }
    IEnumerator TransitionToRestartSequence()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeImage.color;

            // Tối dần màn hình (Dùng unscaledDeltaTime vì game đang dừng)
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
        }

        // QUAN TRỌNG: Trả lại thời gian về 1 để Scene mới chạy được
        Time.timeScale = 1f;

        // Tải lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}