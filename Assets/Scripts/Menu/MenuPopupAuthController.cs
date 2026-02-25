using System.Collections;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MenuPopupAuthController : MonoBehaviour
{
    private enum ViewState
    {
        Login,
        Register,
    }

    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private ShopServerSO _shopServer;

    [SerializeField]
    private Button _btnRegister;

    [SerializeField]
    private Button _btnLogin;

    [SerializeField]
    private Button _btnClose;

    [SerializeField]
    private TMP_InputField _inpLoginUsername;

    [SerializeField]
    private TMP_InputField _inpLoginPassword;

    [SerializeField]
    private TMP_InputField _inpRegisterUsername;

    [SerializeField]
    private TMP_InputField _inpRegisterPassword;

    [SerializeField]
    private TMP_InputField _inpRegisterConfirmPassword;

    [SerializeField]
    private GameObject _loginView;

    [SerializeField]
    private GameObject _registerView;

    private ViewState _viewState;

    private void Awake()
    {
        _viewState = ViewState.Login;
    }

    private void Start()
    {
        _btnLogin.onClick.AddListener(OnBtnLoginClick);
        _btnRegister.onClick.AddListener(OnBtnRegisterClick);
        _btnClose.onClick.AddListener(OnBtnCloseClick);
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            StartCoroutine(SyncUserInfo());
        }
    }

    private void OnBtnLoginClick()
    {
        if (_viewState != ViewState.Login)
        {
            _loginView.SetActive(true);
            _registerView.SetActive(false);
            _viewState = ViewState.Login;
            return;
        }

        StartCoroutine(HandleLogin());
    }

    private IEnumerator HandleLogin()
    {
        using UnityWebRequest request = new(_shopServer.GetLoginUrl(), "POST");

        string username = _inpLoginUsername.text;
        string password = _inpLoginPassword.text;
        LoginRequestPayload loginData = new(username, password);
        string json = JsonConvert.SerializeObject(loginData);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UserInfoResponsePayload response = JsonConvert.DeserializeObject<UserInfoResponsePayload>(request.downloadHandler.text);
            if (response.Success)
            {
                PlayerPrefs.SetString(nameof(PlayerPrefsKeys.S_UserId), response.Data.Id);
                PlayerPrefs.SetString(nameof(PlayerPrefsKeys.S_UserName), response.Data.Username);
                PlayerPrefs.SetInt(nameof(PlayerPrefsKeys.I_Coin), response.Data.Coin);
                _inpLoginUsername.SetTextWithoutNotify("");
                _inpLoginPassword.SetTextWithoutNotify("");
                _utils.HidePopup();
            }
        }
    }

    private void OnBtnRegisterClick()
    {
        if (_viewState != ViewState.Register)
        {
            _loginView.SetActive(false);
            _registerView.SetActive(true);
            _viewState = ViewState.Register;
            return;
        }

        StartCoroutine(HandleRegister());
    }

    private IEnumerator HandleRegister()
    {
        using UnityWebRequest request = new(_shopServer.GetRegisterUrl(), "POST");

        string username = _inpRegisterUsername.text;
        string password = _inpRegisterPassword.text;
        string passwordConfirm = _inpRegisterConfirmPassword.text;
        RegisterRequestPayload loginData = new(username, password, passwordConfirm);
        string json = JsonConvert.SerializeObject(loginData);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UserInfoResponsePayload response = JsonConvert.DeserializeObject<UserInfoResponsePayload>(request.downloadHandler.text);
            if (response.Success)
            {
                PlayerPrefs.SetString(nameof(PlayerPrefsKeys.S_UserId), response.Data.Id);
                PlayerPrefs.SetString(nameof(PlayerPrefsKeys.S_UserName), response.Data.Username);
                PlayerPrefs.SetInt(nameof(PlayerPrefsKeys.I_Coin), response.Data.Coin);
                _inpRegisterUsername.SetTextWithoutNotify("");
                _inpRegisterPassword.SetTextWithoutNotify("");
                _inpRegisterConfirmPassword.SetTextWithoutNotify("");
                _utils.HidePopup();
            }
        }
    }

    private void OnBtnCloseClick()
    {
        _utils.HidePopup();
    }

    private IEnumerator SyncUserInfo()
    {
        string userId = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserId));

        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(userId))
        {
            yield break;
        }

        using UnityWebRequest request = UnityWebRequest.Get(_shopServer.GetUserInfoUrl(userId));

        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            UserInfoResponsePayload response = JsonConvert.DeserializeObject<UserInfoResponsePayload>(request.downloadHandler.text);
            if (response.Success)
            {
                PlayerPrefs.SetInt(nameof(PlayerPrefsKeys.I_Coin), response.Data.Coin);
            }
        }

    }

    [JsonObject]
    private struct LoginRequestPayload
    {
        [JsonProperty("username")]
        public string Username { get; private set; }

        [JsonProperty("password")]
        public string Password { get; private set; }

        public LoginRequestPayload(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    [JsonObject]
    private struct RegisterRequestPayload
    {
        [JsonProperty("username")]
        public string Username { get; private set; }

        [JsonProperty("password")]
        public string Password { get; private set; }

        [JsonProperty("passwordConfirm")]
        public string PasswordConfirm { get; private set; }

        public RegisterRequestPayload(string username, string password, string passwordConfirm)
        {
            Username = username;
            Password = password;
            PasswordConfirm = passwordConfirm;
        }
    }

    [JsonObject]
    private struct UserInfoResponsePayload
    {
        [JsonProperty("success")]
        public bool Success { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }

        [JsonProperty("data")]
        public UserData Data { get; private set; }
    }

    [JsonObject]
    private struct UserData
    {
        [JsonProperty("_id")]
        public string Id { get; private set; }

        [JsonProperty("username")]
        public string Username { get; private set; }

        [JsonProperty("coin")]
        public int Coin { get; private set; }
    }
}
