using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class QuestPopupManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject questPopup; 
    
    [Tooltip("Kéo object Progress_Text vào đây để quản lý ẩn/hiện")]
    public GameObject progressTextUI; 

    [Header("Nút bấm")]
    public Button acceptButton; 

    public static bool isGameStarted = false;

    void Start()
    {
        isGameStarted = false; 
        questPopup.SetActive(false);
        if (progressTextUI != null) progressTextUI.SetActive(false);

        acceptButton.onClick.AddListener(OnAcceptClicked);
        StartCoroutine(ShowPopup());
    }

    IEnumerator ShowPopup()
    {
        yield return new WaitForSeconds(3f);
        questPopup.SetActive(true); 
    }

    void OnAcceptClicked()
    {
        questPopup.SetActive(false); 
        
        isGameStarted = true; 
        if (progressTextUI != null) progressTextUI.SetActive(true);
        
        Debug.Log("Người chơi đã Accept! Game chính thức bắt đầu!");
    }
}