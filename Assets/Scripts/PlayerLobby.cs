using UnityEngine;
using Mirror;
using Steamworks;

public class PlayerLobby : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleDisplayNameChanged))]
    public string DisplayName;

    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady;

    public override void OnStartClient()
    {
        SteamLobby steamLobby = FindFirstObjectByType<SteamLobby>();
        
        if (steamLobby != null) steamLobby.ShowOnlineLobbyUI();
        
        // Gọi UpdateUI ngay lập tức
        if (LobbyUIManager.Instance != null) 
        {
            LobbyUIManager.Instance.UpdateUI();
        }
    }

    public override void OnStartAuthority()
    {
        if (SteamManager.Initialized)
        {
            string steamName = SteamFriends.GetPersonaName();
            CmdSetDisplayName(steamName);
        }
    }

    public override void OnStopClient()
    {
        // Nếu là máy mình thì tắt UI về menu
        if (isLocalPlayer)
        {
            SteamLobby steamLobby = FindFirstObjectByType<SteamLobby>();
            
            if (steamLobby != null) steamLobby.ReturnToMainMenu();
        }
        
        // Cập nhật lại UI để xóa slot của người vừa thoát
        if (LobbyUIManager.Instance != null) 
        {
            LobbyUIManager.Instance.Invoke(nameof(LobbyUIManager.Instance.UpdateUI), 0.1f);
        }
    }

    [Command]
    private void CmdSetDisplayName(string name) 
    { 
        DisplayName = name; 
        RpcUpdateUI();
    }

    [Command]
    public void CmdToggleReady() 
    { 
        IsReady = !IsReady; 
        RpcUpdateUI();
    }

    [ClientRpc]
    void RpcUpdateUI()
    {
        if (LobbyUIManager.Instance != null) LobbyUIManager.Instance.UpdateUI();
    }

    void HandleDisplayNameChanged(string old, string newName) => UpdateUI();
    void HandleReadyStatusChanged(bool old, bool newStatus) => UpdateUI();

    private void UpdateUI()
    {
        if (LobbyUIManager.Instance != null) LobbyUIManager.Instance.UpdateUI();
    }
}