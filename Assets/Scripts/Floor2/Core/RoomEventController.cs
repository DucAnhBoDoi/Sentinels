using UnityEngine;
using TMPro; // Thư viện cho TextMeshPro
using System.Collections;

public class RoomEventController : MonoBehaviour
{
    [Header("Đối tượng điều khiển")]
    public GameObject gate;            // Kéo Gate_Main vào đây
    public TextMeshProUGUI timerText;  // Kéo CountdownText vào đây
    public EnemySpawner spawner;       // Kéo Pipe_Spawners vào đây

    [Header("Cài đặt")]
    public int countdownSeconds = 5;
    private bool hasTriggered = false;

    void Start() {
        // Đảm bảo quái vật không ra ngay từ đầu
        if (spawner != null) spawner.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other) {
        // Kiểm tra nếu là Player bước vào
        if (other.CompareTag("Player") && !hasTriggered) {
            hasTriggered = true;
            StartCoroutine(StartEventRoutine());
        }
    }

    IEnumerator StartEventRoutine() {
        // 1. Đóng cửa sập
        if (gate != null) gate.SetActive(true);

        // 2. Chạy đếm ngược
        timerText.gameObject.SetActive(true);
        for (int i = countdownSeconds; i > 0; i--) {
            timerText.text = "QUÁI VẬT XUẤT HIỆN SAU: " + i;
            yield return new WaitForSeconds(1f);
        }

        timerText.text = "CHIẾN ĐẤU!!!";
        yield return new WaitForSeconds(1f);
        timerText.gameObject.SetActive(false);

        // 3. Kích hoạt bầy quái từ 8 miệng ống
        if (spawner != null) spawner.enabled = true;
    }
}