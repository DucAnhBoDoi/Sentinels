using System;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LobbyNetworkDiscovery))]
public class PopupRoomController : MonoBehaviour
{
    private enum RoomMode
    {
        Online,
        Local,
    }

    [SerializeField]
    private LobbyRoom _lobbyRoomPrefab;

    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private Button _btnJoinRoom;

    [SerializeField]
    private Button _btnOnline;

    [SerializeField]
    private Button _btnLocal;

    [SerializeField]
    private Button _btnRefresh;

    [SerializeField]
    private Button _btnClose;

    [SerializeField]
    private Transform _roomContainer;

    private RoomMode _mode;

    private LobbyNetworkDiscovery _networkDiscovery;

    private void Awake()
    {
        _networkDiscovery = GetComponent<LobbyNetworkDiscovery>();
    }

    private void Start()
    {
        _btnJoinRoom.onClick.AddListener(OnBtnJoinRoomClick);
        _btnOnline.onClick.AddListener(OnBtnOnlineClick);
        _btnLocal.onClick.AddListener(OnBtnLocalClick);
        _btnRefresh.onClick.AddListener(OnBtnRefreshClick);
        _btnClose.onClick.AddListener(OnBtnCloseClick);
        _networkDiscovery.OnServerFound += OnLocalDiscoveryServerFound;
    }

    private void OnBtnJoinRoomClick()
    {
        DiscoverRooms(_mode);
    }

    private void OnBtnOnlineClick()
    {
        SetRoomMode(RoomMode.Online);
    }

    private void OnBtnLocalClick()
    {
        SetRoomMode(RoomMode.Local);
    }

    private void OnBtnRefreshClick()
    {
        switch (_mode)
        {
            case RoomMode.Local:
                {
                    ClearRooms();
                    _networkDiscovery.ClientBroadcast(new());
                }
                break;
            case RoomMode.Online:
            default:
                break;
        }
    }

    private void OnBtnCloseClick()
    {
        ClosePopup();
    }

    private void OnLocalDiscoveryServerFound(IPEndPoint endpoint, DiscoveryResponseData response)
    {
        LobbyRoom room = Instantiate(_lobbyRoomPrefab);
        room.ServerName = response.ServerName;
        room.ServerPort = response.Port;
        room.ServerAddress = endpoint.Address;
        room.OnRoomJoin += OnLocalRoomJoin;
        room.transform.SetParent(_roomContainer.transform, false);
    }

    private void OnLocalRoomJoin()
    {
        ClosePopup();
        _utils.ShowLobbyCoop();
        _utils.LobbyCoopSetCoopModeLocal();
    }

    private void SetRoomMode(RoomMode mode)
    {
        if (_mode == mode)
        {
            return;
        }

        _mode = mode;
        ClearRooms();
        DiscoverRooms(_mode);
    }

    private void ClearRooms()
    {
        foreach (Transform room in _roomContainer.transform)
        {
            Destroy(room.gameObject);
        }
    }

    private void DiscoverRooms(RoomMode mode)
    {
        switch (mode)
        {
            case RoomMode.Local:
                {
                    _networkDiscovery.StartClient();
                    _networkDiscovery.ClientBroadcast(new());
                }
                break;
            case RoomMode.Online:
                {
                    _networkDiscovery.StopDiscovery();
                }
                break;
            default:
                break;
        }
    }

    private void ClosePopup()
    {
        _utils.HidePopup();
        _networkDiscovery.StopDiscovery();
    }

    // [SerializeField]
    // private MenuSceneUtils _utils;
    //
    // [Header("Buttons")]
    // [SerializeField]
    // private Button _btnClose;
    //
    // [SerializeField]
    // private Button _btnTabOnline;
    //
    // [SerializeField]
    // private Button _btnTabLocal;
    //
    // [SerializeField]
    // private Button _btnLoad;
    //
    // [Header("Content Containers")]
    // [SerializeField]
    // private GameObject _contentOnline;
    //
    // [SerializeField]
    // private GameObject _contentLocal;
    //
    // private bool _isOnlineMode = true;
    //
    // private void Start()
    // {
    //     _btnClose.onClick.AddListener(() => _utils.HidePopup());
    //
    //     _btnTabOnline.onClick.AddListener(OnBtnTabOnlineClick);
    //     _btnTabLocal.onClick.AddListener(OnBtnTabLocalClick);
    //
    //     if (_btnLoad != null)
    //     {
    //         _btnLoad.onClick.AddListener(OnBtnLoadClick);
    //     }
    //
    //     OnBtnTabOnlineClick();
    // }
    //
    // private void OnBtnTabOnlineClick()
    // {
    //     _isOnlineMode = true;
    //
    //     _contentOnline.SetActive(true);
    //     _contentLocal.SetActive(false);
    //
    //     _btnTabOnline.interactable = false;
    //     _btnTabLocal.interactable = true;
    //
    //     Debug.Log("Đã chuyển sang Tab ONLINE");
    //     // TODO: Sau này sẽ gọi hàm tìm phòng Online ở đây
    //     // LobbyManager.Instance.RefreshList();
    // }
    //
    // private void OnBtnTabLocalClick()
    // {
    //     _isOnlineMode = false;
    //
    //     _contentOnline.SetActive(false);
    //     _contentLocal.SetActive(true);
    //
    //     _btnTabOnline.interactable = true;
    //     _btnTabLocal.interactable = false;
    //
    //     Debug.Log("Đã chuyển sang Tab LOCAL");
    //     // TODO: Sau này sẽ gọi hàm tìm phòng LAN ở đây
    //     // NetworkDiscovery.Search();
    // }
    //
    // private void OnBtnLoadClick()
    // {
    //     if (_isOnlineMode)
    //     {
    //         Debug.Log("Đang tải lại danh sách Online...");
    //     }
    //     else
    //     {
    //         Debug.Log("Đang quét lại mạng LAN...");
    //     }
    // }
}
