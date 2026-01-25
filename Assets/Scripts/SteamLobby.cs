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

    [Header("UI Reference (Auto Updated)")]
    public GameObject mainLayout;
    public GameObject lobbyLayout;
    public GameObject lobbyOnline;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ lại Object này khi chuyển Scene
        }
        else if (Instance != this)
        {
            // Nếu phát hiện Object trùng (do load lại Scene), hủy cái mới đi
            Destroy(gameObject);
            return;
        }
        // ----------------------------

        networkManager = GetComponent<NetworkManager>();
    }

    // --- FIX: Hàm cập nhật UI mới từ MainMenuController ---
    public void SetUIPanels(GameObject main, GameObject lobby, GameObject online)
    {
        mainLayout = main;
        lobbyLayout = lobby;
        lobbyOnline = online;
    }

    private void Start()
    {
        if (!SteamManager.Initialized) { return; }
        
        // Chỉ đăng ký Callback nếu chưa đăng ký
        if (lobbyCreated == null)
        {
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        }
    }

    public void HostLobby()
    {
        // FIX: Kiểm tra null trước khi dùng
        if (mainLayout != null) mainLayout.SetActive(false);

        networkManager.maxConnections = 2;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    public void InviteFriends()
    {
        if (currentLobbyID.m_SteamID == 0) return;
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyID);
    }

    public void LeaveLobby()
    {
        // 1. Ngắt kết nối Mirror
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            networkManager.StopHost();
        }
        else
        {
            networkManager.StopClient();
        }

        // 2. Rời Steam Lobby
        if (currentLobbyID.m_SteamID != 0)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyID);
            currentLobbyID = new CSteamID(0);
        }
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
        if (NetworkServer.active) { return; }

        currentLobbyID = new CSteamID(callback.m_ulSteamIDLobby);
        string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyID, HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();
        
        ShowOnlineLobbyUI();
    }

    public void ShowOnlineLobbyUI()
    {
        // FIX: Kiểm tra null an toàn
        if (mainLayout != null) mainLayout.SetActive(false);
        if (lobbyLayout != null) lobbyLayout.SetActive(false);

        if (lobbyOnline != null)
        {
            lobbyOnline.SetActive(true);
            // Reset UI Manager để xóa thông tin cũ
            if (LobbyUIManager.Instance != null) LobbyUIManager.Instance.ResetUI();
        }
    }

    public void ReturnToMainMenu()
    {
        if (lobbyOnline != null) lobbyOnline.SetActive(false);
        if (mainLayout != null) mainLayout.SetActive(true);
    }
}