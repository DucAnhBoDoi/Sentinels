using UnityEngine;
using UnityEngine.UI;

public class MenuPopupShopController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private ShopServerSO _shopServer;

    [SerializeField]
    private Button _btnBuyCoin;

    [SerializeField]
    private Button _btnClose;

    private void Start()
    {
        _btnBuyCoin.onClick.AddListener(OnBtnBuyCoinClick);
        _btnClose.onClick.AddListener(OnBtnCloseClick);
    }

    private void OnBtnBuyCoinClick()
    {
        string shopUrl = _shopServer.GetShopUrl(PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserId)));
        Application.OpenURL(shopUrl);
    }

    private void OnBtnCloseClick()
    {
        _utils.HidePopup();
    }
}
