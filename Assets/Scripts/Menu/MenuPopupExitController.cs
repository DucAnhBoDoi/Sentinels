using UnityEngine;
using UnityEngine.UI;

public class MenuPopupExitController : MonoBehaviour
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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnBtnNoClick()
    {
        _utils.HidePopup();
    }
}
