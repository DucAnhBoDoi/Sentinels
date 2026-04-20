using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Ẩn bảng lúc đầu
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Gán sự kiện cho nút
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        // DỪNG TOÀN BỘ GAME (Vật lý, AI, Animation)
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (Floor1Manager.Instance != null) Floor1Manager.Instance.RestartLevelWithFade();
        else if (Floor2Manager.Instance != null) Floor2Manager.Instance.RestartLevelWithFade(); // THÊM DÒNG NÀY CHO TẦNG 2
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        if (Floor1Manager.Instance != null) Floor1Manager.Instance.QuitToMenuWithFade("MenuScene");
        else if (Floor2Manager.Instance != null) Floor2Manager.Instance.QuitToMenuWithFade("MenuScene"); // THÊM DÒNG NÀY CHO TẦNG 2
        else SceneManager.LoadScene("MenuScene");
    }
}