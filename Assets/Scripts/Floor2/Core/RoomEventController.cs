using UnityEngine;
using TMPro; 
using System.Collections;

public class RoomEventController : MonoBehaviour
{
    [Header("Đối tượng điều khiển")]
    public GameObject gate;            
    public TextMeshProUGUI timerText;  
    public EnemySpawner spawner;       

    [Header("UI cần ẩn/hiện")]
    [Tooltip("Kéo Core_HealthBar vào đây để giấu nó lúc đầu")]
    public GameObject coreHealthBar; 

    [Header("Cài đặt")]
    public int countdownSeconds = 5;
    private bool hasTriggered = false;

    void Start() {
        if (spawner != null) spawner.enabled = false;
        
        // Tắt chữ đếm ngược 5s lúc mới vào game
        if (timerText != null) timerText.gameObject.SetActive(false);

        // TẮT LUÔN THANH MÁU CỦA LÕI LÚC MỚI VÀO GAME
        if (coreHealthBar != null) coreHealthBar.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player") && !hasTriggered) {
            hasTriggered = true;
            StartCoroutine(StartEventRoutine());
        }
    }

    IEnumerator StartEventRoutine() {
        // 1. Đóng cửa sập
        if (gate != null) gate.SetActive(true);

        // 2. Chạy đếm ngược 5 giây
        timerText.gameObject.SetActive(true);
        for (int i = countdownSeconds; i > 0; i--) {
            timerText.text = "ENEMIES INCOMING: " + i;
            yield return new WaitForSeconds(1f);
        }

        timerText.text = "FIGHT!!!";
        yield return new WaitForSeconds(1f);
        timerText.gameObject.SetActive(false);

        // 3. BẬT THANH MÁU CỦA LÕI LÊN
        if (coreHealthBar != null) coreHealthBar.SetActive(true);

        // 4. Kích hoạt bầy quái
        if (spawner != null) spawner.enabled = true;

        // 5. Bắt đầu tính giờ của Tầng 2 (Floor2Manager sẽ tự động hiện TimerText lên)
        if (Floor2Manager.Instance != null) Floor2Manager.Instance.StartTimer();
    }
}