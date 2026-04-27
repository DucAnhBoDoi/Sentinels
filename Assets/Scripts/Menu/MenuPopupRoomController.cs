using System.Collections.Generic; 
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

    private Dictionary<IPAddress, LobbyRoom> _discoveredRooms = new Dictionary<IPAddress, LobbyRoom>();

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

    private void Update()
    {
        _btnOnline.interactable = _mode != RoomMode.Online;
        _btnLocal.interactable = _mode != RoomMode.Local;
    }

    private void OnBtnJoinRoomClick()
    {
        ClearRooms();
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
        if (_discoveredRooms.ContainsKey(endpoint.Address))
        {
            return;
        }

        LobbyRoom room = Instantiate(_lobbyRoomPrefab);
        room.ServerName = response.ServerName;
        room.ServerPort = response.Port;
        room.ServerAddress = endpoint.Address;
        room.OnRoomJoin += OnLocalRoomJoin;
        room.transform.SetParent(_roomContainer.transform, false);

        _discoveredRooms.Add(endpoint.Address, room);
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
        _discoveredRooms.Clear();

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
}