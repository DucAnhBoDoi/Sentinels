using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
    public const int MAX_PLAYER_COUNT = 2;

    [SerializeField] private MenuSceneUtils _utils;

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
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

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
        {
            return;
        }

        StartCoroutine(WaitForShutdownAndShowMain());
    }

    private IEnumerator WaitForShutdownAndShowMain()
    {
        while (NetworkManager.Singleton && NetworkManager.Singleton.ShutdownInProgress)
        {
            yield return null;
        }

        if (SceneManager.GetActiveScene().name != nameof(SceneNames.MenuScene))
        {
            SceneManager.LoadScene(nameof(SceneNames.MenuScene));
        }
        else
        {
            _utils.ShowMainLayout();
        }
    }
}