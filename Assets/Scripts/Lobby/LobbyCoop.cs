using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private void Awake()
    {
        _lobbyPlayers = new(GameNetworkManager.MAX_PLAYER_COUNT);
    }

    private void Start()
    {
        _btnBack.onClick.AddListener(OnBtnBackClick);
        _btnStartGame.onClick.AddListener(OnBtnStartGameClick);
    }

    private void Update()
    {
        if ((NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost) ||
                _lobbyPlayers.Count != GameNetworkManager.MAX_PLAYER_COUNT)
        {
            if (_btnStartGame.interactable) _btnStartGame.interactable = false;
            return;
        }

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

    private void OnEnable()
    {
        CleanUpLobby();

        if (_lobbyAction == LobbyAction.Join)
        {
            NetworkManager.Singleton.StartClient();
        }

        if (_coopMode == CoopMode.Local)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnLocalClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnLocalClientDisconnect;

            if (_lobbyAction == LobbyAction.Create)
            {
                UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                transport.SetConnectionData("127.0.0.1", transport.ConnectionData.Port, "0.0.0.0");
                NetworkManager.Singleton.StartHost();
            }
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnLocalClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnLocalClientDisconnect;
        }
        CleanUpLobby();
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