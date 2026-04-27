using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; 

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

    // ---> THÊM HÀM NÀY ĐỂ XÓA TRÍ NHỚ KHI QUAY VỀ MENU <---
    public static void ResetQuestState()
    {
        hasAcceptedOnce = false;
        lastSceneName = "";
        isGameStarted = false;
    }

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (lastSceneName != currentScene)
        {
            hasAcceptedOnce = false; 
            lastSceneName = currentScene; 
        }

        acceptButton.onClick.AddListener(OnAcceptClicked);

        if (hasAcceptedOnce)
        {
            SkipQuestAndStart();
        }
        else
        {
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
        
        if (!hasAcceptedOnce) 
        {
            questPopup.SetActive(true); 
        }
    }

    void OnAcceptClicked()
    {
        hasAcceptedOnce = true; 
        questPopup.SetActive(false); 
        isGameStarted = true; 
        if (progressTextUI != null) progressTextUI.SetActive(true);
    }
}