using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

public class LobbyNetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    public event UnityAction<IPEndPoint, DiscoveryResponseData> OnServerFound;

    private string _playerName;

    private string GetServerName()
    {
        if (NetworkManager.Singleton == null)
        {
            return "[ERROR]";
        }

        return
            $"[LAN] - {_playerName} - [{NetworkManager.Singleton.ConnectedClients.Count}/{GameNetworkManager.MAX_PLAYER_COUNT}]";
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost && !IsRunning)
            {
                StartServer();
                _playerName = NetworkManager
                    .Singleton
                    .LocalClient
                    .PlayerObject
                    .GetComponent<PlayerObject>()
                    .NetUsername
                    .Value
                    .ToString();
            }
            else if (!NetworkManager.Singleton.IsHost && IsRunning && IsServer)
            {
                StopDiscovery();
            }
        }
    }

    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast,
        out DiscoveryResponseData response)
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