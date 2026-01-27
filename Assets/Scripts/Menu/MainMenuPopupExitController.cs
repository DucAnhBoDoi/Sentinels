using UnityEngine;
using UnityEngine.UI;

public class MainMenuPopupExitController : MonoBehaviour
{
    [SerializeField]
    private Button _btnYes;

    [SerializeField]
    private Button _btnNo;

    [SerializeField]
    private GameObject _popupExit;

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
        _popupExit.SetActive(false);
    }
}
