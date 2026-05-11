using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Scripts.Floor3.Core; // THÊM DÒNG 1
using Unity.Netcode; // BẮT BUỘC THÊM THƯ VIỆN MẠNG
using System.Collections; // BẮT BUỘC THÊM ĐỂ CHẠY COROUTINE

// ĐỔI TỪ MonoBehaviour SANG NetworkBehaviour
public class GameOverManager : NetworkBehaviour
{
    public static GameOverManager Instance;
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button quitButton;

    // --- THÊM KHU VỰC CẤU HÌNH ÂM THANH ---
    [Header("Cấu hình Âm thanh Game Over")]
    public AudioSource gameOverAudio;
    public float fadeOutTime = 1.5f; 
    private bool isRestarting = false; // Khóa chống bấm đúp nút

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

        isRestarting = false; // Đảm bảo mở khóa nút mỗi khi hiện bảng

        // TẮT NÚT ĐỐI VỚI CLIENT (CHỈ HOST MỚI BẤM ĐƯỢC)
        if (!IsServer)
        {
            if (restartButton != null) restartButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;
        }

        // --- LOGIC ÂM THANH: TẮT NHẠC NỀN & BẬT NHẠC GAME OVER ---
        GameObject bgmManager = GameObject.Find("BGM_Manager");
        if (bgmManager != null)
        {
            AudioSource bgmSource = bgmManager.GetComponent<AudioSource>();
            if (bgmSource != null) bgmSource.Stop();
        }

        if (gameOverAudio != null)
        {
            gameOverAudio.volume = 1f; // Trả về max volume
            gameOverAudio.time = 1f;
            gameOverAudio.Play();
        }
    }

    public void RestartGame()
    {
        // BẢO MẬT: Chặn không cho Client gọi hàm này HOẶC nút đang bị khóa
        if (!IsServer || isRestarting) return; 

        // SỬA Ở ĐÂY: Host gọi lệnh mạng để cả Host và Client cùng chạy hiệu ứng
        RestartGameClientRpc(); 
    }

    // THÊM LỆNH CLIENT RPC CHO RESTART
    [ClientRpc]
    private void RestartGameClientRpc()
    {
        if (isRestarting) return;
        isRestarting = true; // Khóa nút trên mọi máy
        StartCoroutine(FadeOutMusicAndRestart()); // Gọi coroutine đợi nhạc tắt
    }

    private IEnumerator FadeOutMusicAndRestart()
    {
        // 1. TỪ TỪ LÀM NHỎ NHẠC (Cả Host và Client đều chạy đoạn này)
        if (gameOverAudio != null)
        {
            float startVol = gameOverAudio.volume;
            float timer = 0f;

            while (timer < fadeOutTime)
            {
                timer += Time.unscaledDeltaTime; 
                gameOverAudio.volume = Mathf.Lerp(startVol, 0f, timer / fadeOutTime);
                yield return null;
            }
            gameOverAudio.Stop();
        }

        // 2. NHẠC ĐÃ TẮT -> CHẠY CODE RESTART GỐC CỦA BẠN (CHỈ HOST ĐƯỢC CHẠY LỆNH NÀY)
        Time.timeScale = 1f;
        if (IsServer)
        {
            if (Floor1Manager.Instance != null) Floor1Manager.Instance.RestartLevelWithFade();
            else if (Floor2Manager.Instance != null) Floor2Manager.Instance.RestartLevelWithFade();
            else if (Floor3Manager.Instance != null) Floor3Manager.Instance.RestartLevelWithFade();
            else NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name,LoadSceneMode.Single);
        }
    }

    public void QuitToMenu()
    {
        // BẢO MẬT: Chặn không cho Client gọi hàm này
        if (!IsServer || isRestarting) return;

        // SỬA Ở ĐÂY: Host gọi lệnh mạng để cả Host và Client cùng chạy hiệu ứng tắt nhạc
        QuitToMenuClientRpc();
    }

    // THÊM LỆNH CLIENT RPC CHO QUIT
    [ClientRpc]
    private void QuitToMenuClientRpc()
    {
        if (isRestarting) return;
        isRestarting = true;
        StartCoroutine(FadeOutMusicAndQuit()); // Làm mờ nhạc khi ra Menu luôn cho mượt
    }

    private IEnumerator FadeOutMusicAndQuit()
    {
        // 1. TỪ TỪ LÀM NHỎ NHẠC (Cả Host và Client đều chạy)
        if (gameOverAudio != null)
        {
            float startVol = gameOverAudio.volume;
            float timer = 0f;

            while (timer < fadeOutTime)
            {
                timer += Time.unscaledDeltaTime;
                gameOverAudio.volume = Mathf.Lerp(startVol, 0f, timer / fadeOutTime);
                yield return null;
            }
            gameOverAudio.Stop();
        }

        // 2. CHẠY CODE QUIT GỐC CỦA BẠN (CHỈ HOST ĐƯỢC GỌI)
        Time.timeScale = 1f;
        if (IsServer)
        {
            if (Floor1Manager.Instance != null) Floor1Manager.Instance.QuitToMenuWithFade("MenuScene");
            else if (Floor2Manager.Instance != null) Floor2Manager.Instance.QuitToMenuWithFade("MenuScene");
            else if (Floor3Manager.Instance != null) Floor3Manager.Instance.QuitToMenuWithFade("MenuScene");
            else 
            {
                if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("MenuScene");
            }
        }
    }
}