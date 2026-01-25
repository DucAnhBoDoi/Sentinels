using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_Text _txtTitle;
    [SerializeField] private Button _buttonContinue;
    [SerializeField] private Button _buttonShop;
    [SerializeField] private GameObject _mainLayout;

    [Header("New Game")]
    [SerializeField] private Button _buttonNewGame;
    [SerializeField] private GameObject _lobbyLayout;
    [SerializeField] private Button _lobbyLayoutButtonOnlineCoop;
    [SerializeField] private Button _lobbyLayoutButtonBack;

    [SerializeField] private GameObject _lobbyOnline;
    [SerializeField] private Button _lobbyOnlineButtonBack;

    [Header("Options")]
    [SerializeField] private Button _buttonOptions;
    [SerializeField] private GameObject _popupOptions;
    [SerializeField] private Button _popupOptionsButtonBack;
    [SerializeField] private Button _popupOptionsButtonSave;

    [Header("Exit")]
    [SerializeField] private Button _buttonExit;
    [SerializeField] private GameObject _popupExit;
    [SerializeField] private Button _popupExitButtonNo;
    [SerializeField] private Button _popupExitButtonYes;

    private const float ButtonHoverTargetX = 25;
    private const float ButtonHoverTransitionDuration = 0.25f;

    private void Awake()
    {
        // Logic gán sự kiện cũ giữ nguyên
        if (!_buttonContinue.interactable)
        {
            TMP_Text btnText = _buttonContinue.GetComponentInChildren<TMP_Text>();
            if(btnText) btnText.color = Color.grey;
        }
        else
        {
            AddHoverEffect(_buttonContinue);
        }

        AddHoverEffect(_buttonNewGame);
        AddHoverEffect(_buttonShop);
        AddHoverEffect(_buttonOptions);
        AddHoverEffect(_buttonExit);
        AddHoverEffect(_lobbyLayoutButtonOnlineCoop);
        AddHoverEffect(_lobbyLayoutButtonBack);

        _buttonNewGame.onClick.AddListener(OnNewGameButtonClick);
        
        _lobbyLayoutButtonOnlineCoop.onClick.AddListener(OnLobbyLayoutButtonOnlineCoopClick);
        
        _lobbyLayoutButtonBack.onClick.AddListener(OnLobbyLayoutButtonBackClick);
        _lobbyOnlineButtonBack.onClick.AddListener(OnLobbyOnlineButtonBackClick);

        _buttonOptions.onClick.AddListener(OnOptionsButtonClick);
        _popupOptionsButtonBack.onClick.AddListener(OnPopupOptionsButtonBackClick);
        _popupOptionsButtonSave.onClick.AddListener(OnPopupOptionsButtonSaveClick);

        _buttonExit.onClick.AddListener(OnExitButtonClick);
        _popupExitButtonNo.onClick.AddListener(OnPopupExitButtonNoClick);
        _popupExitButtonYes.onClick.AddListener(OnPopupExitButtonYesClick);
    }

    private void Start()
    {
        
        if (SteamLobby.Instance != null)
        {
            SteamLobby.Instance.SetUIPanels(_mainLayout, _lobbyLayout, _lobbyOnline);
        }
    }
    // ----------------------------------------------------

    private void OnNewGameButtonClick()
    {
        _mainLayout.SetActive(false);
        _lobbyLayout.SetActive(true);
        ResetTextAnimations(_mainLayout);
    }

    private void OnLobbyLayoutButtonOnlineCoopClick()
    {
        if(SteamLobby.Instance != null)
        {
             SteamLobby.Instance.HostLobby(); 
        }

    }

    private void OnLobbyLayoutButtonBackClick()
    {
        _mainLayout.SetActive(true);
        _lobbyLayout.SetActive(false);
        ResetTextAnimations(_lobbyLayout);
    }

    private void OnLobbyOnlineButtonBackClick()
    {
        _txtTitle.gameObject.SetActive(true);
        _lobbyLayout.SetActive(true);
        _lobbyOnline.SetActive(false);
    }

    // Helper rút gọn code
    private void ResetTextAnimations(GameObject container)
    {
        foreach (TMP_Text text in container.GetComponentsInChildren<TMP_Text>())
        {
            DOTween.Kill(text);
            text.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    // ... (Phần còn lại giữ nguyên: OnOptions, OnExit, HoverEffect) ...
    private void OnOptionsButtonClick() => _popupOptions.SetActive(true);
    private void OnPopupOptionsButtonBackClick() => _popupOptions.SetActive(false);
    private void OnPopupOptionsButtonSaveClick() => _popupOptions.SetActive(false);
    private void OnExitButtonClick() => _popupExit.SetActive(true);
    private void OnPopupExitButtonNoClick() => _popupExit.SetActive(false);
    
    private void OnPopupExitButtonYesClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void AddHoverEffect(Button button)
    {
        if(button == null) return;
        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
        TMP_Text btnText = button.GetComponentInChildren<TMP_Text>();
        if (btnText == null) return;

        EventTrigger.Entry pointerEnterEntry = new() { eventID = EventTriggerType.PointerEnter };
        pointerEnterEntry.callback.AddListener((_) => OnButtonHoverIn(btnText));

        EventTrigger.Entry pointerExitEntry = new() { eventID = EventTriggerType.PointerExit };
        pointerExitEntry.callback.AddListener((_) => OnButtonHoverOut(btnText));

        trigger.triggers.Add(pointerEnterEntry);
        trigger.triggers.Add(pointerExitEntry);
    }

    private void OnButtonHoverIn(TMP_Text btnText)
    {
        btnText.rectTransform.DOAnchorPosX(ButtonHoverTargetX, ButtonHoverTransitionDuration).SetId(btnText);
    }

    private void OnButtonHoverOut(TMP_Text btnText)
    {
        btnText.rectTransform.DOAnchorPosX(0, ButtonHoverTransitionDuration).SetId(btnText);
    }
}