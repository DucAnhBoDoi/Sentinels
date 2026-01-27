using UnityEngine;
using UnityEngine.UI;

public class MainMenuRoomLayoutController : MonoBehaviour
{
    [SerializeField]
    private Button _btnCreateRoom;

    [SerializeField]
    private Button _btnJoinRoom;

    [SerializeField]
    private Button _btnBack;

    [SerializeField]
    private GameObject _mainLayout;

    [SerializeField]
    private GameObject _roomLayout;

    [SerializeField]
    private GameObject _createRoomLayout;

    private void Start()
    {
        _btnCreateRoom.onClick.AddListener(OnBtnCreateRoomClick);
        _btnBack.onClick.AddListener(OnBtnBackClick);
    }

    private void OnBtnCreateRoomClick()
    {
        _roomLayout.SetActive(false);
        _createRoomLayout.SetActive(true);
    }

    private void OnBtnBackClick()
    {
        _mainLayout.SetActive(true);
        _roomLayout.SetActive(false);
    }
}
