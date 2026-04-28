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
    public event UnityAction<LobbyRoom> OnOnlineRoomJoin;

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

    // --- THÊM BIẾN CHO ONLINE ---
    [HideInInspector]
    public bool IsOnlineRoom = false;

    [HideInInspector]
    public string OnlineLobbyId;

    private void Start()
    {
        _txtServerName.SetText(ServerName);
        _btnJoin.onClick.AddListener(OnBtnJoinClick);
    }

    private void OnBtnJoinClick()
    {
        if (IsOnlineRoom)
        {
            OnOnlineRoomJoin?.Invoke(this); 
        }
        else
        {
            UnityTransport transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            transport.SetConnectionData(ServerAddress.ToString(), ServerPort);
            OnRoomJoin?.Invoke();
        }
    }
}