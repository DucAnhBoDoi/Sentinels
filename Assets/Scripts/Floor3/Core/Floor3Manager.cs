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

    // Biến mạng đồng bộ trạng thái hoàn thành màn chơi
    private NetworkVariable<bool> isLevelComplete = new NetworkVariable<bool>(false);
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnEnable()  { GameContext.OnLevelComplete += LevelComplete; }
    void OnDisable() { GameContext.OnLevelComplete -= LevelComplete; }

    public override void OnNetworkSpawn()
    {
        if (!playerA) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (!playerB) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        if (IsServer)
        {
            isLevelComplete.Value = false;
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            StartCoroutine(FadeFromBlack());
        }
    }

    public void LevelComplete()
    {
        if (IsServer)
        {
            isLevelComplete.Value = true;
            // SỬA: Báo log phím ENTER
            Debug.Log("<color=green>ĐÃ HỘ TỐNG THÀNH CÔNG! BẤM PHÍM [ENTER] ĐỂ QUA TẦNG 4!</color>");

            // ==========================================
            // THÊM: GỌI HIỆU ỨNG UI QUEST COMPLETE ĐỒNG BỘ MẠNG
            // ==========================================
            if (QuestUIManager.Instance != null)
            {
                QuestUIManager.Instance.TriggerQuestCompleteNetwork();
            }
        }
    }

    void Update()
    {
        // Chỉ Server mới có quyền check điều kiện qua màn
        if (!IsServer || !isLevelComplete.Value || isTransitioning) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // SỬA: Đổi từ digit4Key sang enterKey
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

        // --- THÊM: TÌM NHẠC NỀN ĐỂ CHUẨN BỊ LÀM NHỎ ---
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

                // --- THÊM: ÉP NHẠC NỀN NHỎ DẦN ---
                if (bgmSource != null)
                {
                    bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                }

                yield return null; 
            }
        }
        
        // Đảm bảo nhạc tắt hẳn khi màn đen xì
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