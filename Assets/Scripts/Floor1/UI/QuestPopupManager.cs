using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestPopupManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject questPopup; 
    public GameObject progressTextUI; 

    [Header("Nút bấm")]
    public Button acceptButton; 

    public static bool isGameStarted = false;
    
    // BIẾN QUAN TRỌNG: Lưu trạng thái đã chấp nhận Quest (không bị reset khi load scene)
    public static bool hasAcceptedOnce = false;

    void Start()
    {
        acceptButton.onClick.AddListener(OnAcceptClicked);

        // KIỂM TRA: Nếu đã từng Accept trước đó (Restart)
        if (hasAcceptedOnce)
        {
            SkipQuestAndStart();
        }
        else
        {
            // Nếu là lần đầu vào game (hoặc từ Menu chính vào)
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
        isGameStarted = true; 
        if (progressTextUI != null) progressTextUI.SetActive(true);
    }
}