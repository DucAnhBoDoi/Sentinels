using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _txtTitle;

    [SerializeField]
    private Button _buttonContinue;

    [SerializeField]
    private Button _buttonShop;

    [SerializeField]
    private GameObject _mainLayout;

    [Header("New Game")]
    [SerializeField]
    private Button _buttonNewGame;

    [SerializeField]
    private GameObject _lobbyLayout; // Menu chọn Online/Offline

    [SerializeField]
    private Button _lobbyLayoutButtonOnlineCoop;

    [SerializeField]
    private Button _lobbyLayoutButtonBack;

    // --- ĐÃ XÓA: _lobbyOnline và _lobbyOnlineButtonBack ---
    // Lý do: MainMenu không được phép can thiệp vào bên trong phòng Online.
    // Việc tắt/bật phòng Online là việc của SteamLobby.cs

    [Header("Options")]
    [SerializeField]
    private Button _buttonOptions;

    [SerializeField]
    private GameObject _popupOptions;

    [SerializeField]
    private Button _popupOptionsButtonBack;

    [SerializeField]
    private Button _popupOptionsButtonSave;

    [Header("Exit")]
    [SerializeField]
    private Button _buttonExit;

    [SerializeField]
    private GameObject _popupExit;

    [SerializeField]
    private Button _popupExitButtonNo;

    [SerializeField]
    private Button _popupExitButtonYes;

    private const float ButtonHoverTargetX = 25;
    private const float ButtonHoverTransitionDuration = 0.25f;

    private void Awake()
    {
        if (!_buttonContinue.interactable)
        {
            TMP_Text btnText = _buttonContinue.GetComponentInChildren<TMP_Text>();
            if (btnText) btnText.color = Color.grey;
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

        // FIX: Hàm bấm nút Online Coop sẽ gọi lệnh khác
        _lobbyLayoutButtonOnlineCoop.onClick.AddListener(OnLobbyLayoutButtonOnlineCoopClick);

        _lobbyLayoutButtonBack.onClick.AddListener(OnLobbyLayoutButtonBackClick);

        // --- ĐÃ XÓA Listener của _lobbyOnlineButtonBack ---

        _buttonOptions.onClick.AddListener(OnOptionsButtonClick);
        _popupOptionsButtonBack.onClick.AddListener(OnPopupOptionsButtonBackClick);
        _popupOptionsButtonSave.onClick.AddListener(OnPopupOptionsButtonSaveClick);

        _buttonExit.onClick.AddListener(OnExitButtonClick);
        _popupExitButtonNo.onClick.AddListener(OnPopupExitButtonNoClick);
        _popupExitButtonYes.onClick.AddListener(OnPopupExitButtonYesClick);
    }

    private void OnNewGameButtonClick()
    {
        _mainLayout.SetActive(false);
        _lobbyLayout.SetActive(true);
        KillTweensIn(_mainLayout); // Tối ưu code gom vào hàm
    }

    // --- FIX QUAN TRỌNG NHẤT Ở ĐÂY ---
    private void OnLobbyLayoutButtonOnlineCoopClick()
    {
        // Thay vì MainMenu tự bật UI Lobby lên (sai), 
        // nó sẽ bảo thằng SteamLobby: "Ê, tạo phòng Host đi!"
        if (SteamLobby.Instance != null)
        {
            // SteamLobby sẽ tự lo việc ẩn MainMenu và hiện LobbyOnline
            SteamLobby.Instance.HostLobby();

            // Ẩn cái menu chọn chế độ đi
            _lobbyLayout.SetActive(false);
            _txtTitle.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Không tìm thấy SteamLobby Instance!");
        }

        KillTweensIn(_lobbyLayout);
    }

    private void OnLobbyLayoutButtonBackClick()
    {
        _mainLayout.SetActive(true);
        _lobbyLayout.SetActive(false);
        KillTweensIn(_lobbyLayout);
    }

    // --- ĐÃ XÓA hàm OnLobbyOnlineButtonBackClick ---
    // Nút Back trong Lobby Online giờ đây hoàn toàn do LobbyUIManager quản lý.

    private void OnOptionsButtonClick()
    {
        _popupOptions.SetActive(true);
    }

    private void OnPopupOptionsButtonBackClick()
    {
        _popupOptions.SetActive(false);
    }

    private void OnPopupOptionsButtonSaveClick()
    {
        _popupOptions.SetActive(false);
    }

    private void OnExitButtonClick()
    {
        _popupExit.SetActive(true);
    }

    private void OnPopupExitButtonNoClick()
    {
        _popupExit.SetActive(false);
    }

    private void OnPopupExitButtonYesClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Hàm hỗ trợ để code gọn hơn
    private void KillTweensIn(GameObject container)
    {
        foreach (TMP_Text text in container.GetComponentsInChildren<TMP_Text>())
        {
            DOTween.Kill(text);
            text.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    private void AddHoverEffect(Button button)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        TMP_Text btnText = button.GetComponentInChildren<TMP_Text>();
        if (btnText == null) return;

        EventTrigger.Entry pointerEnterEntry = new()
        {
            eventID = EventTriggerType.PointerEnter,
        };
        pointerEnterEntry.callback.AddListener((_) => OnButtonHoverIn(btnText));

        EventTrigger.Entry pointerExitEntry = new()
        {
            eventID = EventTriggerType.PointerExit
        };
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