using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class LobbyLANBroadcaster : MonoBehaviour
{
    [SerializeField]
    private LANBroadcastConfigSO _config;

    [SerializeField]
    private float _broadcastInterval = 1f;

    private IPEndPoint _broadcastEndpoint;
    private UnityTransport _transport;

    private UdpClient _udpClient;

    private float _broadcastTimer;

    private void OnEnable()
    {
        _broadcastTimer = 0;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        }
    }

    private void Awake()
    {
        _broadcastEndpoint = new(IPAddress.Broadcast, _config.BroadcastPort);
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    private void Start()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void Update()
    {
        if (_udpClient == null)
        {
            return;
        }

        _broadcastTimer -= Time.deltaTime;

        if (_broadcastTimer > 0)
        {
            return;
        }

        _broadcastTimer = _broadcastInterval;

        string ip = "127.0.0.1";

        foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork && IPAddress.IsLoopback(address))
            {
                ip = address.ToString();
                break;
            }
        }

        string message = $"{ip}|{_transport.ConnectionData.Port}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        _udpClient.Send(data, data.Length, _broadcastEndpoint);
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        _udpClient = new()
        {
            EnableBroadcast = true,
        };
    }

    private void OnServerStopped(bool isClient)
    {
        _udpClient?.Close();
        _udpClient = null;
    }
}
