using UnityEngine;
using UnityEngine.UI;

public class MenuCoopLayoutController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private Button _btnOnline;

    [SerializeField]
    private Button _btnLocal;

    [SerializeField]
    private Button _btnBack;

    private void Start()
    {
        _btnOnline.onClick.AddListener(OnBtnOnlineClick);
        _btnLocal.onClick.AddListener(OnBtnLocalClick);
        _btnBack.onClick.AddListener(OnBtnBackClick);
    }

    private void OnBtnOnlineClick()
    {
        _utils.ShowLobbyOnline();
    }

    private void OnBtnLocalClick()
    {
        _utils.ShowLobbyOnline();
    }

    private void OnBtnBackClick()
    {
        _utils.ShowMainLayout();
    }
}
