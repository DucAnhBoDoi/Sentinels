using TMPro;
using UnityEngine;

public class MenuPrefsObserver : MonoBehaviour
{
    [SerializeField]
    private GameObject _loggedInLayout;

    [SerializeField]
    private GameObject _loggedOutLayout;

    [SerializeField]
    private TMP_Text _txtUsername;

    [SerializeField]
    private TMP_Text _txtCoin;

    private string _username;
    private int _coin;

    private void Start()
    {
        _txtUsername.SetText(_username);
        _txtCoin.SetText(_coin.ToString());
    }

    private void Update()
    {
        string id = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserId));
        string username = "";
        int coin = 0;

        if (!string.IsNullOrEmpty(id) && !string.IsNullOrWhiteSpace(id))
        {
            if (!_loggedInLayout.activeSelf)
            {
                _loggedInLayout.SetActive(true);
            }
            if (_loggedOutLayout.activeSelf)
            {
                _loggedOutLayout.SetActive(false);
            }

            username = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName));
            coin = PlayerPrefs.GetInt(nameof(PlayerPrefsKeys.I_Coin));
        }
        else
        {
            if (_loggedInLayout.activeSelf)
            {
                _loggedInLayout.SetActive(false);
            }
            if (!_loggedOutLayout.activeSelf)
            {
                _loggedOutLayout.SetActive(true);
            }
        }

        if (_username != username)
        {
            _txtUsername.SetText(username);
            _username = username;
        }

        if (_coin != coin)
        {
            _txtCoin.SetText(coin.ToString());
            _coin = coin;
        }
    }
}
