using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LobbyLANListener : MonoBehaviour
{
    [SerializeField]
    private LANBroadcastConfigSO _config;

    private UdpClient _udpClient;
    private Thread _thread;

    private void OnEnable()
    {
        _udpClient = new(_config.BroadcastPort);

        _thread = new(ListenLoop)
        {
            IsBackground = true
        };
        _thread.Start();
    }

    private void OnDisable()
    {
        _udpClient.Close();
        _udpClient = null;

        _thread.Abort();
        _thread = null;
    }

    private void ListenLoop()
    {
        IPEndPoint endPoint = new(IPAddress.Any, _config.BroadcastPort);

        while (true)
        {
            byte[] data = _udpClient.Receive(ref endPoint);
            string message = Encoding.UTF8.GetString(data);
            string[] parts = message.Split('|');

            string ip = "";
            string port = "";

            if (parts.Length >= 2)
            {
                ip = parts[0];
                port = parts[1];
            }
            Debug.Log(ip + ":" + port);
        }
    }
}
