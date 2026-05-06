using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
// --- THÊM THƯ VIỆN ONLINE ---
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay; // THÊM RELAY
using Unity.Services.Relay.Models; // THÊM RELAY
using System.Threading.Tasks;
using Unity.Netcode; // Bắt buộc cho Transport
using Unity.Netcode.Transports.UTP;

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

    private List<string> _discoveredOnlineRoomIDs = new List<string>();

    private void Awake()
    {
        _networkDiscovery = GetComponent<LobbyNetworkDiscovery>();
    }

    private async void Start()
    {
        _btnJoinRoom.onClick.AddListener(OnBtnJoinRoomClick);
        _btnOnline.onClick.AddListener(OnBtnOnlineClick);
        _btnLocal.onClick.AddListener(OnBtnLocalClick);
        _btnRefresh.onClick.AddListener(OnBtnRefreshClick);
        _btnClose.onClick.AddListener(OnBtnCloseClick);
        _networkDiscovery.OnServerFound += OnLocalDiscoveryServerFound;

        // Bắt đầu đăng nhập Unity Services khi vừa mở game
        await InitializeUnityServicesAsync();
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
                {
                    ClearRooms();
                    FetchOnlineLobbies();
                }
                break;
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

        room.IsOnlineRoom = false; // Đánh dấu đây là phòng Local

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
        _discoveredOnlineRoomIDs.Clear();

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
                    FetchOnlineLobbies();
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

    // ==========================================================
    // KHU VỰC THÊM MỚI: XỬ LÝ UNITY LOBBY & RELAY (ONLINE MODE)
    // ==========================================================

    private async Task InitializeUnityServicesAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"<color=yellow>[Online] Đăng nhập Unity thành công! ID: {AuthenticationService.Instance.PlayerId}</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Online] Lỗi kết nối Unity Services: {e.Message}");
        }
    }

    private async void FetchOnlineLobbies()
    {
        Debug.Log("<color=cyan>[Online Mode] Đang kết nối máy chủ Unity để tải danh sách phòng...</color>");
        try
        {
            // Cài đặt lọc tìm phòng (Chỉ hiện phòng còn trống)
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;
            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            Debug.Log($"<color=green>[Online] Tìm thấy {response.Results.Count} phòng đang mở!</color>");

            foreach (Lobby lobby in response.Results)
            {
                if (_discoveredOnlineRoomIDs.Contains(lobby.Id))
                {
                    continue; // Bỏ qua nếu phòng đã hiện trên màn hình
                }

                // Sinh ra UI Phòng y hệt như Local
                LobbyRoom room = Instantiate(_lobbyRoomPrefab);
                int currentPlayers = lobby.MaxPlayers - lobby.AvailableSlots;

                // Ghép thêm chữ [ONLINE] và số lượng người vào tên phòng
                room.ServerName = $"[NET] - {lobby.Name} - [{currentPlayers}/{lobby.MaxPlayers}]";
                room.OnlineLobbyId = lobby.Id;

                room.IsOnlineRoom = true; // Đánh dấu là phòng Online

                room.OnOnlineRoomJoin += OnOnlineRoomJoinHandler; // Lắng nghe sự kiện Join Online
                room.transform.SetParent(_roomContainer.transform, false);

                _discoveredOnlineRoomIDs.Add(lobby.Id);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[Online] Lỗi tải danh sách phòng: {e.Message}");
        }
    }

    // --- LOGIC CLIENT JOIN PHÒNG ONLINE ---
    private async void OnOnlineRoomJoinHandler(LobbyRoom room)
    {
        Debug.Log($"<color=magenta>Chuẩn bị kết nối xuyên tường lửa vào phòng ID: {room.OnlineLobbyId}</color>");
        try
        {
            // 1. Vào Unity Lobby lấy cái JoinCode do Host giấu ở đó
            Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(room.OnlineLobbyId);
            string joinCode = lobby.Data["JoinCode"].Value;

            // 2. Nộp JoinCode cho máy chủ Relay để kết nối
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 3. Setup mạng Unity Netcode xuyên tường lửa
            UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            transport.SetClientRelayData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            // 4. Mở cửa sổ sảnh chờ
            ClosePopup();
            _utils.ShowLobbyCoop();
            _utils.LobbyCoopSetCoopModeOnline();

            LobbyCoop coopUI = Object.FindAnyObjectByType<LobbyCoop>(FindObjectsInactive.Include);
            if (coopUI != null)
            {
                coopUI.SetOnlineLobbyId(room.OnlineLobbyId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Online] Lỗi khi Join phòng: {e.Message}");
        }
    }
}