using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCoop : MonoBehaviour
{
    private enum CoopMode
    {
        Local,
        Online,
    }

    private enum LobbyAction
    {
        Join,
        Create,
    }

    private const int MaxPlayerCount = 2;

    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private LobbyPlayer _lobbyPlayerPrefab;

    [SerializeField]
    private NetworkObject _player1SpawnPoint;

    [SerializeField]
    private NetworkObject _player2SpawnPoint;

    [SerializeField]
    private Button _btnBack;

    [SerializeField]
    private Button _btnStartGame;

    private CoopMode _coopMode;
    private LobbyAction _lobbyAction;

    private List<LobbyPlayer> _lobbyPlayers;

    private void Awake()
    {
        _lobbyPlayers = new(MaxPlayerCount);
    }

    private void Start()
    {
        _btnBack.onClick.AddListener(OnBtnBackClick);
        _btnStartGame.onClick.AddListener(OnBtnStartGameClick);
    }

    private void Update()
    {
        if ((NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost) ||
                _lobbyPlayers.Count != MaxPlayerCount)
        {
            if (_btnStartGame.interactable)
            {
                _btnStartGame.interactable = false;
            }
            return;
        }

        int readyCount = 0;

        foreach (LobbyPlayer player in _lobbyPlayers)
        {
            if (player.NetIsReady.Value)
            {
                readyCount++;
            }
        }

        if (readyCount == MaxPlayerCount && !_btnStartGame.interactable)
        {
            _btnStartGame.interactable = true;
        }
        else if (readyCount != MaxPlayerCount && _btnStartGame.interactable)
        {
            _btnStartGame.interactable = false;
        }
    }

    private void OnEnable()
    {
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
    }

    private void OnBtnBackClick()
    {
        NetworkManager.Singleton.Shutdown();
    }

    private void OnBtnStartGameClick()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(nameof(SceneNames.GamePlayFloor1), UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnLocalClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.Count > MaxPlayerCount)
        {
            NetworkManager.Singleton.Shutdown();
            return;
        }

        LobbyPlayer player = Instantiate(_lobbyPlayerPrefab);
        player.NetworkObject.SpawnWithOwnership(clientId, true);

        NetworkObject spawnLocation = _player2SpawnPoint;

        if (clientId == NetworkManager.ServerClientId)
        {
            spawnLocation = _player1SpawnPoint;
        }

        player.NetworkObject.TrySetParent(spawnLocation, false);

        _lobbyPlayers.Add(player);
    }

    private void OnLocalClientDisconnect(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        int removeIdx = -1;

        for (int i = 0; i < _lobbyPlayers.Count; i++)
        {
            if (_lobbyPlayers[i].OwnerClientId == clientId)
            {
                removeIdx = i;
                break;
            }
        }

        if (removeIdx != -1)
        {
            if (_lobbyPlayers[removeIdx].IsSpawned)
            {
                _lobbyPlayers[removeIdx].NetworkObject.Despawn();
            }
            _lobbyPlayers.RemoveAt(removeIdx);
        }
    }

    public void SetCoopModeLocal()
    {
        _coopMode = CoopMode.Local;
    }

    public void SetCoopModeOnline()
    {
        _coopMode = CoopMode.Online;
    }

    public void SetLobbyActionJoin()
    {
        _lobbyAction = LobbyAction.Join;
    }

    public void SetLobbyActionCreate()
    {
        _lobbyAction = LobbyAction.Create;
    }
}
