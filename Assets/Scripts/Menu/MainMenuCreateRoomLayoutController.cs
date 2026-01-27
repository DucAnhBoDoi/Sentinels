using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuCreateRoomLayoutController : MonoBehaviour
{
    [SerializeField]
    private Button _btnOnline;

    [SerializeField]
    private Button _btnLocal;

    [SerializeField]
    private Button _btnBack;

    [SerializeField]
    private TMP_Text _txtTitle;

    [SerializeField]
    private GameObject _roomLayout;

    [SerializeField]
    private GameObject _createRoomLayout;

    [SerializeField]
    private GameObject _lobbyOnline;

    private void Start()
    {
        _btnOnline.onClick.AddListener(OnBtnOnlineClick);
        _btnLocal.onClick.AddListener(OnBtnLocalClick);
        _btnBack.onClick.AddListener(OnBtnBackClick);
    }

    private void OnBtnOnlineClick()
    {
        _txtTitle.gameObject.SetActive(false);
        _createRoomLayout.SetActive(false);
        _lobbyOnline.SetActive(true);
    }

    private void OnBtnLocalClick()
    {
        _txtTitle.gameObject.SetActive(false);
        _createRoomLayout.SetActive(false);
        _lobbyOnline.SetActive(true);
    }

    private void OnBtnBackClick()
    {
        _createRoomLayout.SetActive(false);
        _roomLayout.SetActive(true);
    }
}
