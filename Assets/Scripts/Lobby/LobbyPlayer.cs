using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{
    [HideInInspector] public NetworkVariable<bool> NetIsReady = new(writePerm: NetworkVariableWritePermission.Owner);

    [HideInInspector]
    public NetworkVariable<FixedString64Bytes> NetPlayerName = new(writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField] private TMP_Text _txtPlayerName;

    [SerializeField] private Button _btnPlayerReady;
    private Image _imgBtnPlayerReady;

    private Color _colorReady;
    private Color _colorNotReady;

    private void Awake()
    {
        _imgBtnPlayerReady = _btnPlayerReady.GetComponent<Image>();
        _colorReady = Color.green;
        _colorNotReady = Color.white;
        NetPlayerName.OnValueChanged += (_, playerName) => _txtPlayerName.SetText(playerName.Value);
    }

    private void Update()
    {
        if (NetIsReady.Value && _imgBtnPlayerReady.color != _colorReady)
        {
            _imgBtnPlayerReady.color = _colorReady;
        }
        else if (!NetIsReady.Value && _imgBtnPlayerReady.color != _colorNotReady)
        {
            _imgBtnPlayerReady.color = _colorNotReady;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PlayerObject playerObject = NetworkManager.ConnectedClients[NetworkObject.OwnerClientId].PlayerObject
            .GetComponent<PlayerObject>();

        if (IsOwner)
        {
            NetPlayerName.Value = playerObject.NetUsername.Value;
        }
        else
        {
            _txtPlayerName.SetText(NetPlayerName.Value.ToString());
        }

        _btnPlayerReady.onClick.AddListener(OnBtnPlayerReadyClick);
        _btnPlayerReady.interactable = IsOwner;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _btnPlayerReady.onClick.RemoveListener(OnBtnPlayerReadyClick);
        _btnPlayerReady.interactable = false;
    }

    private void OnBtnPlayerReadyClick()
    {
        if (IsOwner)
        {
            NetIsReady.Value = !NetIsReady.Value;
        }
    }
}