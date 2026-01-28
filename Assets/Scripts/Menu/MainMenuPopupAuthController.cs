using UnityEngine;
using UnityEngine.UI;

public class MainMenuPopupAuthController : MonoBehaviour
{
    [SerializeField]
    private Button _btnRegister;

    [SerializeField]
    private Button _btnLogin;

    [SerializeField]
    private Button _btnClose;

    [SerializeField]
    private GameObject _registerLayout;

    [SerializeField]
    private GameObject _loginLayout;

    [SerializeField]
    private GameObject _authPopup;

    private void Start()
    {
        _btnRegister.onClick.AddListener(OnBtnRegisterClick);
        _btnLogin.onClick.AddListener(OnBtnLoginClick);
        _btnClose.onClick.AddListener(OnBtnCloseClick);
    }

    private void OnBtnRegisterClick()
    {
        _registerLayout.SetActive(true);
        _loginLayout.SetActive(false);
    }

    private void OnBtnLoginClick()
    {
        _registerLayout.SetActive(false);
        _loginLayout.SetActive(true);
    }

    private void OnBtnCloseClick()
    {
        _authPopup.SetActive(false);
    }
}
