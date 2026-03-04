using Unity.Netcode;
using UnityEngine;

public class GameNetworkManager : MonoBehaviour
{
    public const int MAX_PLAYER_COUNT = 2;

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong obj)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.Count > MAX_PLAYER_COUNT)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
