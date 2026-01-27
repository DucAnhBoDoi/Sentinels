using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuLobbyOnlineController : MonoBehaviour
{
    [SerializeField]
    private Button _btnStartGame;

    [SerializeField]
    private Button _btnBack;

    [SerializeField]
    private GameObject _lobbyOnline;

    [SerializeField]
    private GameObject _createRoomLayout;

    [SerializeField]
    private TMP_Text _txtTitle;

    private void Start()
    {
        _btnBack.onClick.AddListener(OnBtnBackClick);
    }

    private void OnBtnBackClick()
    {
        _lobbyOnline.SetActive(false);
        _createRoomLayout.SetActive(true);
        _txtTitle.gameObject.SetActive(true);
    }
}
