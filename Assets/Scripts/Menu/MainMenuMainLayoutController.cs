using UnityEngine;
using UnityEngine.UI;

public class MainMenuMainLayoutController : MonoBehaviour
{
    [SerializeField]
    private Button _btnContinue;

    [SerializeField]
    private Button _btnNewGame;

    [SerializeField]
    private Button _btnShop;

    [SerializeField]
    private Button _btnOptions;

    [SerializeField]
    private Button _btnExit;

    [SerializeField]
    private GameObject _mainLayout;

    [SerializeField]
    private GameObject _roomLayout;

    [SerializeField]
    private GameObject _popupOptions;

    [SerializeField]
    private GameObject _popupExit;

    private void Start()
    {
        _btnNewGame.onClick.AddListener(OnBtnNewGameClick);
        _btnOptions.onClick.AddListener(OnBtnOptionsClick);
        _btnExit.onClick.AddListener(OnBtnExitClick);
    }

    private void OnBtnNewGameClick()
    {
        _mainLayout.SetActive(false);
        _roomLayout.SetActive(true);
    }

    private void OnBtnOptionsClick()
    {
        _popupOptions.SetActive(true);
    }

    private void OnBtnExitClick()
    {
        _popupExit.SetActive(true);
    }
}
