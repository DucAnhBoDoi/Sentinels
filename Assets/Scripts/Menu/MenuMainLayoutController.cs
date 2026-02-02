using UnityEngine;
using UnityEngine.UI;

public class MenuMainLayoutController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private Button _btnContinue;

    [SerializeField]
    private Button _btnNewGame;

    [SerializeField]
    private Button _btnJoinRoom;

    [SerializeField]
    private Button _btnShop;

    [SerializeField]
    private Button _btnOptions;

    [SerializeField]
    private Button _btnExit;

    [SerializeField]
    private Button _btnAuth;

    [SerializeField]
    private Button _btnLogout;

    private void Start()
    {
        _btnNewGame.onClick.AddListener(OnBtnNewGameClick);
        _btnShop.onClick.AddListener(OnBtnShopClick);
        _btnOptions.onClick.AddListener(OnBtnOptionsClick);
        _btnExit.onClick.AddListener(OnBtnExitClick);
        _btnAuth.onClick.AddListener(OnBtnAuthClick);
        _btnLogout.onClick.AddListener(OnBtnLogoutClick);
    }

    private void OnBtnNewGameClick()
    {
        _utils.ShowCoopLayout();
    }

    private void OnBtnShopClick()
    {
        _utils.ShowPopupShop();
    }

    private void OnBtnOptionsClick()
    {
        _utils.ShowPopupOptions();
    }

    private void OnBtnExitClick()
    {
        _utils.ShowPopupExit();
    }

    private void OnBtnAuthClick()
    {
        _utils.ShowPopupAuth();
    }

    private void OnBtnLogoutClick()
    {
        _utils.ShowPopupLogout();
    }
}
