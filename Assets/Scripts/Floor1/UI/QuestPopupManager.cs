using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; // BẮT BUỘC có dòng này để lấy tên Tầng

public class QuestPopupManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject questPopup; 
    public GameObject progressTextUI; 

    [Header("Nút bấm")]
    public Button acceptButton; 

    public static bool isGameStarted = false;
    
    // BIẾN QUAN TRỌNG: Lưu trạng thái đã chấp nhận Quest
    public static bool hasAcceptedOnce = false;

    // BIẾN MỚI: Dùng để check xem đã qua Tầng mới chưa
    private static string lastSceneName = ""; 

    void Start()
    {
        // LẤY TÊN MÀN CHƠI HIỆN TẠI (Ví dụ: GamePlayFloor2)
        string currentScene = SceneManager.GetActiveScene().name;
        
        // KIỂM TRA XEM CÓ VỪA QUA TẦNG KHÔNG
        if (lastSceneName != currentScene)
        {
            hasAcceptedOnce = false; // Reset lại trạng thái để hiện Quest
            lastSceneName = currentScene; // Lưu lại tên Tầng này
        }

        acceptButton.onClick.AddListener(OnAcceptClicked);

        // KIỂM TRA: Nếu đã từng Accept trước đó (Restart cùng 1 tầng)
        if (hasAcceptedOnce)
        {
            SkipQuestAndStart();
        }
        else
        {
            // Nếu là lần đầu vào tầng này
            isGameStarted = false; 
            questPopup.SetActive(false);
            if (progressTextUI != null) progressTextUI.SetActive(false);
            StartCoroutine(ShowPopup());
        }
    }

    void SkipQuestAndStart()
    {
        isGameStarted = true; 
        questPopup.SetActive(false);
        if (progressTextUI != null) progressTextUI.SetActive(true);
        Debug.Log("Restart game: Bỏ qua Quest Popup!");
    }

    IEnumerator ShowPopup()
    {
        // Đợi 3 giây (Lúc này màn hình đang sáng dần lên)
        yield return new WaitForSeconds(3f);
        
        if (!hasAcceptedOnce) // Chỉ hiện nếu chưa từng accept
        {
            questPopup.SetActive(true); 
        }
    }

    void OnAcceptClicked()
    {
        hasAcceptedOnce = true; // Đánh dấu đã accept
        questPopup.SetActive(false); 
        isGameStarted = true; // Bắt đầu cho phép di chuyển
        if (progressTextUI != null) progressTextUI.SetActive(true);
    }
}