using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Scripts.Floor3.Core; // THÊM DÒNG 1

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;

    void Awake() { if (Instance == null) Instance = this; }

    // THÊM ĐOẠN 2: Lắng nghe Robot chết
    void OnEnable()  { GameContext.OnGameOver += HandleRobotDeath; }
    void OnDisable() { GameContext.OnGameOver -= HandleRobotDeath; }
    void HandleRobotDeath(GameOverReason r) { ShowGameOver(); }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToMenu);
    }

    public void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (Floor1Manager.Instance != null) Floor1Manager.Instance.RestartLevelWithFade();
        else if (Floor2Manager.Instance != null) Floor2Manager.Instance.RestartLevelWithFade();
        else if (Floor3Manager.Instance != null) Floor3Manager.Instance.RestartLevelWithFade();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name); // THÊM DÒNG 3: Tự reset Tầng 3
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        if (Floor1Manager.Instance != null) Floor1Manager.Instance.QuitToMenuWithFade("MenuScene");
        else if (Floor2Manager.Instance != null) Floor2Manager.Instance.QuitToMenuWithFade("MenuScene");
        else if (Floor3Manager.Instance != null) Floor3Manager.Instance.QuitToMenuWithFade("MenuScene");
        else SceneManager.LoadScene("MenuScene");
    }
}