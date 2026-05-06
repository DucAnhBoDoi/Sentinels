using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// --- THÊM THƯ VIỆN ONLINE ---
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication; // THÊM ĐỂ ĐỌC ID CỦA CLIENT

public class LobbyCoop : MonoBehaviour
{
    private enum CoopMode { Local, Online }
    private enum LobbyAction { Join, Create }

    [SerializeField] private MenuSceneUtils _utils;
    [SerializeField] private LobbyPlayer _lobbyPlayerPrefab;
    [SerializeField] private Transform _player1SpawnPoint;
    [SerializeField] private Transform _player2SpawnPoint;
    [SerializeField] private Button _btnBack;
    [SerializeField] private Button _btnStartGame;

    private CoopMode _coopMode;
    private LobbyAction _lobbyAction;
    private List<LobbyPlayer> _lobbyPlayers;

    // Biến cho Online
    private float _heartbeatTimer;
    private string _currentLobbyId;

    private void Awake()
    {
        _lobbyPlayers = new(GameNetworkManager.MAX_PLAYER_COUNT);
    }

    private void Start()
    {
        _btnBack.onClick.AddListener(OnBtnBackClick);
        _btnStartGame.onClick.AddListener(OnBtnStartGameClick);
    }

    // --- HÀM MỚI ĐỂ LƯU ID PHÒNG ONLINE MÀ CLIENT VỪA VÀO ---
    public void SetOnlineLobbyId(string lobbyId)
    {
        _currentLobbyId = lobbyId;
    }

    private void Update()
    {
        if ((NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost) ||
                _lobbyPlayers.Count != GameNetworkManager.MAX_PLAYER_COUNT)
        {
            if (_btnStartGame.interactable) _btnStartGame.interactable = false;
        }
        else
        {
            int readyCount = 0;
            foreach (LobbyPlayer player in _lobbyPlayers)
            {
                if (player != null && player.NetIsReady.Value) readyCount++;
            }

            if (readyCount == GameNetworkManager.MAX_PLAYER_COUNT && !_btnStartGame.interactable)
            {
                _btnStartGame.interactable = true;
            }
            else if (readyCount != GameNetworkManager.MAX_PLAYER_COUNT && _btnStartGame.interactable)
            {
                _btnStartGame.interactable = false;
            }
        }

        // --- NHỊP ĐẬP TIM (HEARTBEAT) CHO PHÒNG ONLINE ---
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && _coopMode == CoopMode.Online && !string.IsNullOrEmpty(_currentLobbyId))
        {
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer > 15f)
            {
                _heartbeatTimer = 0f;
                try { LobbyService.Instance.SendHeartbeatPingAsync(_currentLobbyId); } catch { }
            }
        }
    }

    private async void OnEnable()
    {
        CleanUpLobby();
        
        // CHỈ XÓA ID NẾU ĐANG TẠO PHÒNG MỚI (Tránh xóa nhầm ID do Client Join truyền sang)
        if (_lobbyAction == LobbyAction.Create) 
        {
            _currentLobbyId = "";
        }

        if (_lobbyAction == LobbyAction.Join)
        {
            NetworkManager.Singleton.StartClient();
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnLocalClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnLocalClientDisconnect;

        if (_lobbyAction == LobbyAction.Create)
        {
            if (_coopMode == CoopMode.Local)
            {
                UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                transport.SetConnectionData("127.0.0.1", transport.ConnectionData.Port, "0.0.0.0");
                NetworkManager.Singleton.StartHost();
            }
            else if (_coopMode == CoopMode.Online)
            {
                await CreateOnlineRoomAsync();
            }
        }
    }

    private async Task CreateOnlineRoomAsync()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(GameNetworkManager.MAX_PLAYER_COUNT - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            transport.SetHostRelayData(
                alloc.RelayServer.IpV4, 
                (ushort)alloc.RelayServer.Port, 
                alloc.AllocationIdBytes, 
                alloc.Key, 
                alloc.ConnectionData
            );

            string playerName = PlayerPrefs.HasKey(nameof(PlayerPrefsKeys.S_UserName)) ? PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName)) : "Default Player";
            
            CreateLobbyOptions options = new CreateLobbyOptions {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject> {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync($"{playerName}'s Room", GameNetworkManager.MAX_PLAYER_COUNT, options);
            _currentLobbyId = lobby.Id;

            Debug.Log($"<color=green>[Online] Tạo phòng thành công! Tên: {lobby.Name} | JoinCode: {joinCode}</color>");

            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Online] Lỗi tạo phòng: {e.Message}");
        }
    }

    private async void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnLocalClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnLocalClientDisconnect;
        }
        CleanUpLobby();

        // --- XỬ LÝ LÚC HOST XÓA PHÒNG HOẶC CLIENT THOÁT PHÒNG ---
        if (_coopMode == CoopMode.Online && !string.IsNullOrEmpty(_currentLobbyId))
        {
            try 
            { 
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobbyId); 
                }
                else if (AuthenticationService.Instance.IsSignedIn)
                {
                    // Lệnh báo cho máy chủ Unity Lobby trả lại Slot trống
                    await LobbyService.Instance.RemovePlayerAsync(_currentLobbyId, AuthenticationService.Instance.PlayerId);
                    Debug.Log("<color=yellow>[Online] Client đã rời đi và trả lại chỗ trống!</color>");
                }
                _currentLobbyId = ""; 
            } 
            catch { }
        }
    }

    private void CleanUpLobby()
    {
        _lobbyPlayers.Clear();
        if (_player1SpawnPoint != null)
            foreach (Transform child in _player1SpawnPoint) Destroy(child.gameObject);
        if (_player2SpawnPoint != null)
            foreach (Transform child in _player2SpawnPoint) Destroy(child.gameObject);
    }

    private void OnBtnBackClick()
    {
        _btnStartGame.interactable = false;
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
    }

    private void OnBtnStartGameClick()
    {
        if (NetworkManager.Singleton.IsHost)
            NetworkManager.Singleton.SceneManager.LoadScene(nameof(SceneNames.GamePlayFloor1), LoadSceneMode.Single);
    }

    public void RegisterPlayer(LobbyPlayer player)
    {
        if (!_lobbyPlayers.Contains(player))
        {
            _lobbyPlayers.Add(player);
            
            Transform spawnLocation = player.OwnerClientId == NetworkManager.ServerClientId ? _player1SpawnPoint : _player2SpawnPoint;
            
            player.transform.SetParent(spawnLocation, false);
            player.transform.localScale = Vector3.one; 
        }
    }

    public void UnregisterPlayer(LobbyPlayer player)
    {
        if (_lobbyPlayers.Contains(player)) _lobbyPlayers.Remove(player);
    }

    private void OnLocalClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        LobbyPlayer player = Instantiate(_lobbyPlayerPrefab);
        player.NetworkObject.SpawnWithOwnership(clientId, true);
    }

    private void OnLocalClientDisconnect(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost) return;

        foreach (var player in _lobbyPlayers)
        {
            if (player != null && player.OwnerClientId == clientId)
            {
                if (player.NetworkObject != null && player.NetworkObject.IsSpawned)
                    player.NetworkObject.Despawn();
                break;
            }
        }
    }

    public void SetCoopModeLocal() { _coopMode = CoopMode.Local; }
    public void SetCoopModeOnline() { _coopMode = CoopMode.Online; }
    public void SetLobbyActionJoin() { _lobbyAction = LobbyAction.Join; }
    public void SetLobbyActionCreate() { _lobbyAction = LobbyAction.Create; }
}