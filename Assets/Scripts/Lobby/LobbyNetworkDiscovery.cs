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
        if (NetworkManager.Singleton == null)
        {
            return "[ERROR]";
        }
        return $"[LAN] - {playerName} - [{NetworkManager.Singleton.ConnectedClients.Count}/{GameNetworkManager.MAX_PLAYER_COUNT}]";
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            // 1. CHẶN LỖI: Nếu mạng đang trong quá trình tắt (Shutdown) -> Cấm tuyệt đối không được mở Port.
            if (NetworkManager.Singleton.ShutdownInProgress) return;

            // 2. CHẶN LỖI: Chỉ khi Server ĐÃ CHẠY XONG HOÀN TOÀN (IsListening) thì mới được bật phát sóng (StartServer).
            bool isServerReady = NetworkManager.Singleton.IsHost && NetworkManager.Singleton.IsListening;

            if (isServerReady && !IsRunning)
            {
                try 
                {
                    StartServer();
                }
                catch (System.Exception) 
                {
                    // Bọc Try-Catch để nuốt lỗi nếu hệ điều hành Windows bị lag, chưa kịp nhả Port cũ
                }
            }
            else if (!isServerReady && IsRunning && IsServer)
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