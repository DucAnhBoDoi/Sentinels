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
        SteamLobby steamLobby = FindObjectOfType<SteamLobby>();
        if (steamLobby != null) steamLobby.ShowOnlineLobbyUI();
        
        // Cập nhật UI ngay khi vào
        if (LobbyUIManager.Instance != null) LobbyUIManager.Instance.UpdateUI();
    }

    public override void OnStartAuthority()
    {
        if (SteamManager.Initialized)
        {
            CmdSetDisplayName(SteamFriends.GetPersonaName());
        }
    }

    public override void OnStopClient()
    {
        // Nếu mình thoát -> Về Menu
        if (isLocalPlayer)
        {
            SteamLobby steamLobby = FindObjectOfType<SteamLobby>();
            if (steamLobby != null) steamLobby.ReturnToMainMenu();
        }
        
        // Cập nhật UI sau 0.1s để xóa slot
        if (LobbyUIManager.Instance != null) 
            LobbyUIManager.Instance.Invoke(nameof(LobbyUIManager.Instance.UpdateUI), 0.1f);
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