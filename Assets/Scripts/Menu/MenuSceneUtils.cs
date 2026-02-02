using UnityEngine;

public class MenuSceneUtils : MonoBehaviour
{
    [SerializeField]
    private GameObject _mainGroup;

    [SerializeField]
    private GameObject _mainLayout;

    [SerializeField]
    private GameObject _coopLayout;

    [SerializeField]
    private GameObject _popupOptions;

    [SerializeField]
    private GameObject _popupExit;

    [SerializeField]
    private GameObject _popupAuth;

    [SerializeField]
    private GameObject _popupLogout;

    [SerializeField]
    private GameObject _popupShop;

    [SerializeField]
    private GameObject _lobbyOnline;

    private GameObject _activeLayout;
    private GameObject _activePopup;

    private void Start()
    {
        SetActiveLayout(_mainLayout);
        SetActivePopup(null);
    }

    private void SetActiveLayout(GameObject layout)
    {
        if (_activeLayout != null)
        {
            _activeLayout.SetActive(false);
        }

        if (layout != null)
        {
            layout.SetActive(true);
        }

        _activeLayout = layout;
    }

    private void SetActivePopup(GameObject popup)
    {
        if (_activePopup != null)
        {
            _activePopup.SetActive(false);
        }

        if (popup != null)
        {
            popup.SetActive(true);
        }

        _activePopup = popup;
    }

    public void ShowMainLayout()
    {
        _mainGroup.SetActive(true);
        SetActiveLayout(_mainLayout);
    }

    public void ShowCoopLayout()
    {
        _mainGroup.SetActive(true);
        SetActiveLayout(_coopLayout);
    }

    public void ShowPopupOptions()
    {
        SetActivePopup(_popupOptions);
    }

    public void ShowPopupExit()
    {
        SetActivePopup(_popupExit);
    }

    public void ShowPopupAuth()
    {
        SetActivePopup(_popupAuth);
    }

    public void ShowPopupLogout()
    {
        SetActivePopup(_popupLogout);
    }

    public void ShowPopupShop()
    {
        SetActivePopup(_popupShop);
    }

    public void HidePopup()
    {
        SetActivePopup(null);
    }

    public void ShowLobbyOnline()
    {
        _mainGroup.SetActive(false);
        _lobbyOnline.SetActive(true);
    }
}
