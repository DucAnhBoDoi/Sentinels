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
        if (Floor1Manager.Instance != null)
        {
            // Gọi hiệu ứng tối dần rồi mới load lại màn chơi
            Floor1Manager.Instance.RestartLevelWithFade();
        }
        else
        {
            // Phòng hờ nếu Manager lỗi thì vẫn restart được
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void QuitToMenu()
    {
        // Gọi thẳng sang Floor1Manager để xử lý hiệu ứng tối dần
        if (Floor1Manager.Instance != null)
        {
            Floor1Manager.Instance.QuitToMenuWithFade("MenuScene");
        }
        else
        {
            // Phòng hờ nếu không tìm thấy Manager
            Time.timeScale = 1f;
            SceneManager.LoadScene("MenuScene");
        }
    }
}