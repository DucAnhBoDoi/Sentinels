using UnityEngine;
using UnityEngine.UI;

public class MenuPopupLogoutController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private Button _btnYes;

    [SerializeField]
    private Button _btnNo;

    private void Start()
    {
        _btnYes.onClick.AddListener(OnBtnYesClick);
        _btnNo.onClick.AddListener(OnBtnNoClick);
    }

    private void OnBtnYesClick()
    {
        // 1. Xóa thông tin tài khoản lưu trong máy
        PlayerPrefs.DeleteKey(nameof(PlayerPrefsKeys.S_UserId));
        PlayerPrefs.DeleteKey(nameof(PlayerPrefsKeys.S_UserName));
        PlayerPrefs.DeleteKey(nameof(PlayerPrefsKeys.I_Coin));
        PlayerPrefs.Save();

        MenuPopupShopController shop = Object.FindFirstObjectByType<MenuPopupShopController>();
        if (shop != null) 
        {
            shop.ResetShopData();
        }

        // 3. Tắt bảng thông báo
        _utils.HidePopup();
    }

    private void OnBtnNoClick()
    {
        _utils.HidePopup();
    }
}