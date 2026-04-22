using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Scripts.Floor3.Core; // Kết nối với hệ thống của Tầng 3

public class Floor3Manager : MonoBehaviour
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

    private bool isLevelComplete = false;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Lắng nghe sự kiện "Hộ tống thành công" từ GameContext Tầng 3
    void OnEnable()  { GameContext.OnLevelComplete += LevelComplete; }
    void OnDisable() { GameContext.OnLevelComplete -= LevelComplete; }

    void Start()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        // Vừa vào game là sáng dần màn hình lên
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    public void LevelComplete()
    {
        isLevelComplete = true;
        Debug.Log("<color=green>ĐÃ HỘ TỐNG THÀNH CÔNG! CẢ 2 NGƯỜI CHƠI HÃY LẠI GẦN CỬA VÀ BẤM PHÍM [4] ĐỂ QUA TẦNG 4!</color>");
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

        // Bấm phím 4 để qua Tầng 4
        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            if (elevatorDoor == null) return;

            float distA = playerA ? Vector2.Distance(playerA.position, elevatorDoor.position) : float.MaxValue;
            float distB = playerB ? Vector2.Distance(playerB.position, elevatorDoor.position) : float.MaxValue;

            if (distA <= interactDistance && distB <= interactDistance)
            {
                Debug.Log("Cả 2 đã ở cửa! Đang tải Tầng 4...");
                StartCoroutine(TransitionToNextFloor());
            }
            else
            {
                Debug.Log("CẢ 2 NGƯỜI CHƠI phải đứng sát vào Cửa Thang Máy mới đi được!");
            }
        }
    }

    // --- CÁC HÀM HIỆU ỨNG (Giữ nguyên y hệt Tầng 1 và 2) ---

    IEnumerator TransitionToNextFloor()
    {
        isTransitioning = true;
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.deltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        SceneManager.LoadScene("GamePlayFloor4"); // Chuyển sang Tầng 4
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

    IEnumerator TransitionToMenuSequence(string sceneName)
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        Time.timeScale = 1f;
        QuestPopupManager.hasAcceptedOnce = false;
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator TransitionToRestartSequence()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f; Color c = fadeImage.color;
            while (elapsed < fadeDuration) { elapsed += Time.unscaledDeltaTime; c.a = Mathf.Clamp01(elapsed / fadeDuration); fadeImage.color = c; yield return null; }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}