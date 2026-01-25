using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    private NetworkManager networkManager;
    private const string HostAddressKey = "HostAddress";
    private CSteamID currentLobbyID;

    
    private CallResult<LobbyCreated_t> lobbyCreatedCallResult;
    
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    [Header("UI Reference (Auto Updated)")]
    public GameObject mainLayout;
    public GameObject lobbyLayout;
    public GameObject lobbyOnline;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        networkManager = GetComponent<NetworkManager>();
    }

    public void SetUIPanels(GameObject main, GameObject lobby, GameObject online)
    {
        mainLayout = main;
        lobbyLayout = lobby;
        lobbyOnline = online;

        if (lobbyOnline != null)
        {
            Button[] buttons = lobbyOnline.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.Contains("Invite") || btn.name.Contains("invite")) 
                {
                    btn.onClick.RemoveAllListeners(); 
                    btn.onClick.AddListener(InviteFriends); 
                    
                    break; 
                }
            }
        }
    }

    private void Start()
    {
        if (!SteamManager.Initialized) { return; }

        // Khởi tạo CallResult
        lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

        if (gameLobbyJoinRequested == null)
        {
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        }
    }

    public void HostLobby()
    {
        if (mainLayout != null) mainLayout.SetActive(false);

        // Đảm bảo không còn ở trong lobby cũ trước khi tạo mới
        if (currentLobbyID.m_SteamID != 0)
        {
            LeaveLobby();
        }

        networkManager.maxConnections = 2;

        // Gọi Steam tạo phòng và đợi kết quả qua CallResult
        SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
        lobbyCreatedCallResult.Set(handle);
    }

    public void InviteFriends()
    {

        if (currentLobbyID.m_SteamID == 0)
        {
            Debug.LogError("Chưa có Lobby ID! Có thể việc tạo phòng đã thất bại.");
            return;
        }
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
    }

    public void LeaveLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            networkManager.StopHost();
        }
        else
        {
            networkManager.StopClient();
        }

        if (currentLobbyID.m_SteamID != 0)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = new CSteamID(0);
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback, bool bIOFailure)
    {
        if (bIOFailure || callback.m_eResult != EResult.k_EResultOK)
        {
            if (mainLayout != null) mainLayout.SetActive(true);
            return;
        }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(currentLobbyID, HostAddressKey, SteamUser.GetSteamID().ToString());
        
        ShowOnlineLobbyUI();
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) { return; }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
        
        ShowOnlineLobbyUI();
    }

    public void ShowOnlineLobbyUI()
    {
        if (mainLayout != null) mainLayout.SetActive(false);
        if (lobbyLayout != null) lobbyLayout.SetActive(false);

        if (lobbyOnline != null)
        {
            lobbyOnline.SetActive(true);
        }
    }

    public void ReturnToMainMenu()
    {
        LeaveLobby();
        if (lobbyOnline != null) lobbyOnline.SetActive(false);
        if (mainLayout != null) mainLayout.SetActive(true);
    }
}