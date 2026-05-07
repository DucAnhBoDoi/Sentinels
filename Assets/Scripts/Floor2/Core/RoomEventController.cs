using UnityEngine;
using TMPro; 
using System.Collections;
using Unity.Netcode;

public class RoomEventController : NetworkBehaviour
{
    [Header("Đối tượng điều khiển")]
    public GameObject gate;            
    public TextMeshProUGUI timerText;  
    public EnemySpawner spawner;       

    [Header("UI cần ẩn/hiện")]
    public GameObject coreHealthBar; 

    [Header("Cài đặt")]
    public int countdownSeconds = 5;
    
    // BIẾN MẠNG ĐỂ CẢ 2 KHÔNG KÍCH HOẠT 2 LẦN
    private NetworkVariable<bool> hasTriggered = new NetworkVariable<bool>(false);

    void Start() {
        if (spawner != null) spawner.enabled = false;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (coreHealthBar != null) coreHealthBar.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other) {
        // CHỈ SERVER NHẬN DIỆN NGƯỜI CHƠI BƯỚC VÀO
        if (!IsServer) return;

        if (other.CompareTag("Player") && !hasTriggered.Value) {
            hasTriggered.Value = true;
            StartEventClientRpc(); // Báo cho cả 2 máy cùng chạy hiệu ứng
        }
    }

    [ClientRpc]
    void StartEventClientRpc()
    {
        StartCoroutine(StartEventRoutine());
    }

    IEnumerator StartEventRoutine() {
        if (gate != null) gate.SetActive(true);
        timerText.gameObject.SetActive(true);
        for (int i = countdownSeconds; i > 0; i--) {
            timerText.text = "ENEMIES INCOMING: " + i;
            yield return new WaitForSeconds(1f);
        }

        timerText.text = "FIGHT!!!";
        yield return new WaitForSeconds(1f);
        timerText.gameObject.SetActive(false);

        if (coreHealthBar != null) coreHealthBar.SetActive(true);
        
        // ==========================================================
        // --- THÊM LỆNH GỌI NHẠC NỀN DÂNG LÊN SAU KHI ĐẾM NGƯỢC ---
        GameObject bgmManager = GameObject.Find("BGM_Manager");
        if (bgmManager != null)
        {
            AudioSettingsApplier audioApplier = bgmManager.GetComponent<AudioSettingsApplier>();
            // Kích hoạt nhạc (cả Host và Client cùng chạy lệnh này vì đang ở trong ClientRpc)
            if (audioApplier != null) audioApplier.PlayAndFadeIn();
        }
        // ==========================================================

        // CHỈ ĐỂ SERVER BẬT SPAWNER (Để Client không tự đẻ quái)
        if (IsServer && spawner != null) spawner.enabled = true;

        if (Floor2Manager.Instance != null && IsServer) Floor2Manager.Instance.StartTimerServerRpc();
    }
}