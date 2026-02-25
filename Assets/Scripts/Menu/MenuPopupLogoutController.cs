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
        PlayerPrefs.DeleteKey(nameof(PlayerPrefsKeys.S_UserId));
        _utils.HidePopup();
    }

    private void OnBtnNoClick()
    {
        _utils.HidePopup();
    }
}
