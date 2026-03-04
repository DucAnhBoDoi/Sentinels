using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

public class LobbyNetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    public event UnityAction<IPEndPoint, DiscoveryResponseData> OnServerFound;

    private string GetServerName()
    {
        string playerName = "Default Player";
        if (PlayerPrefs.HasKey(nameof(PlayerPrefsKeys.S_UserName)))
        {
            playerName = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName));
        }
        return $"[LAN] - {playerName} - [{NetworkManager.Singleton.ConnectedClients.Count}/{GameNetworkManager.MAX_PLAYER_COUNT}]";
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost && !IsRunning)
            {
                StartServer();
            }
            else if (!NetworkManager.Singleton.IsHost && IsRunning && IsServer)
            {
                StopDiscovery();
            }
        }
    }

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        response = new DiscoveryResponseData()
        {
            ServerName = GetServerName(),
            Port = ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound?.Invoke(sender, response);
    }
}
