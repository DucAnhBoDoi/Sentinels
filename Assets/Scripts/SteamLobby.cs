using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    private NetworkManager networkManager;
    private const string HostAddressKey = "HostAddress";
    private CSteamID currentLobbyID;

    // Callbacks
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    [Header("UI Panels")]
    public GameObject mainLayout;
    public GameObject lobbyLayout;
    public GameObject lobbyOnline;

    // Cờ kiểm soát trạng thái để tránh bấm liên tục
    private bool isLeavingLobby = false;

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

    private void Start()
    {
        if (!SteamManager.Initialized) return;
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        // Chặn nếu đang thoát hoặc mạng đang chạy
        if (isLeavingLobby || NetworkServer.active) return;

        if (mainLayout != null) mainLayout.SetActive(false);
        
        networkManager.maxConnections = 2;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    public void InviteFriends()
    {
        if (currentLobbyID.m_SteamID == 0) return;
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
    }

    // --- HÀM THOÁT PHÒNG SẠCH SẼ ---
    public void LeaveLobby()
    {
        if (isLeavingLobby) return;
        isLeavingLobby = true;

        // 1. Ngắt kết nối Mirror
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            networkManager.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            networkManager.StopClient();
        }

        // 2. Rời Steam Lobby
        if (currentLobbyID.m_SteamID != 0)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = new CSteamID(0);
        }

        // 3. Về Menu chính
        ReturnToMainMenu();

        // 4. Mở khóa sau 0.5s để cho phép Host lại
        Invoke(nameof(ResetLeavingFlag), 0.5f);
    }

    private void ResetLeavingFlag()
    {
        isLeavingLobby = false;
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
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
        if (NetworkServer.active) return;

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
        
        ShowOnlineLobbyUI();
    }

    public void ShowOnlineLobbyUI()
    {
        if (mainLayout) mainLayout.SetActive(false);
        if (lobbyLayout) lobbyLayout.SetActive(false);
        
        if (lobbyOnline) 
        {
            lobbyOnline.SetActive(true);
            // GỌI RESET NGAY LẬP TỨC KHI BẬT UI
            if(LobbyUIManager.Instance != null) LobbyUIManager.Instance.ResetUI();
        }
    }

    public void ReturnToMainMenu()
    {
        if (lobbyOnline) lobbyOnline.SetActive(false);
        if (mainLayout) mainLayout.SetActive(true);
    }
}