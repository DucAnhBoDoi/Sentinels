using UnityEngine;

public class Floor3UIManager : MonoBehaviour
{
    public static Floor3UIManager Instance;

    [Header("Các bảng UI (ĐÃ TẮT SẴN TRONG INSPECTOR)")]
    public GameObject topicSelectionPanel;
    public GameObject robotHUD;
    public GameObject proximityHUD;
    public GameObject quizPanel;

    private bool hasTriggeredTopic = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ĐƯỢC GỌI KHI LẠI GẦN ROBOT
    public void ShowTopicSelection()
    {
        if (hasTriggeredTopic) return;
        hasTriggeredTopic = true;

        Time.timeScale = 0f; // Dừng game để chọn chủ đề
        if (topicSelectionPanel != null) topicSelectionPanel.SetActive(true); // BẬT LÊN
    }

    // ĐƯỢC GỌI KHI BẤM "START MISSION"
    public void StartMission()
    {
        Time.timeScale = 1f; // Chạy lại game
        if (topicSelectionPanel != null) topicSelectionPanel.SetActive(false); // Xài xong thì tắt đi
        
        // BẬT máu robot và cảnh báo khoảng cách lên
        if (robotHUD != null) robotHUD.SetActive(true);
        if (proximityHUD != null) proximityHUD.SetActive(true);
    }
}