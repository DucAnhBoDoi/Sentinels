using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyRoom : MonoBehaviour
{
    public event UnityAction OnRoomJoin;

    [SerializeField]
    private TMP_Text _txtServerName;

    [SerializeField]
    private Button _btnJoin;

    [HideInInspector]
    public string ServerName;

    [HideInInspector]
    public IPAddress ServerAddress;

    [HideInInspector]
    public ushort ServerPort;

    private void Start()
    {
        _txtServerName.SetText(ServerName);
        _btnJoin.onClick.AddListener(OnBtnJoinClick);
    }

    private void OnBtnJoinClick()
    {
        UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.SetConnectionData(ServerAddress.ToString(), ServerPort);
        OnRoomJoin?.Invoke();
    }
}
