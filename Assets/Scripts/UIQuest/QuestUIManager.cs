using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class QuestUIManager : NetworkBehaviour
{
    public static QuestUIManager Instance;

    [Header("UI Cần hiển thị")]
    public GameObject completePopup; // Kéo Complete_Popup vào đây
    public GameObject waypointIcon;  // Kéo Waypoint_Icon vào đây

    [Header("Cài đặt thời gian")]
    public float popupDisplayTime = 3f; // Thời gian hiện Popup trước khi tắt

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Server gọi hàm này để ra lệnh cho toàn bộ máy tính trong phòng
    public void TriggerQuestCompleteNetwork()
    {
        if (IsServer)
        {
            ShowQuestCompleteClientRpc();
        }
    }

    // Lệnh này bắt buộc TẤT CẢ mọi người (Host + Client) phải chạy
    [ClientRpc]
    private void ShowQuestCompleteClientRpc()
    {
        StartCoroutine(QuestCompleteSequence());
    }

    private IEnumerator QuestCompleteSequence()
    {
        // 1. Hiện Popup Hoàn Thành
        if (completePopup != null) completePopup.SetActive(true);

        // 2. Chờ 3 giây để người chơi đọc
        yield return new WaitForSeconds(popupDisplayTime);

        // 3. Tắt Popup đi
        if (completePopup != null) completePopup.SetActive(false);

        // 4. Bật Dấu chấm than lên chỉ đường
        if (waypointIcon != null) waypointIcon.SetActive(true);
    }
}