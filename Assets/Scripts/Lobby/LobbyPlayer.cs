using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<bool> NetIsReady = new(false, writePerm: NetworkVariableWritePermission.Owner);

    [HideInInspector]
    public NetworkVariable<FixedString32Bytes> NetPlayerName = new(
        "", 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

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
    }

    public override void OnNetworkSpawn()
    {
        LobbyCoop lobby = Object.FindFirstObjectByType<LobbyCoop>();
        if (lobby != null)
        {
            lobby.RegisterPlayer(this);
        }

        _btnPlayerReady.interactable = IsOwner;

        if (IsOwner)
        {
            string playerName = "Default Player";
            if (PlayerPrefs.HasKey(nameof(PlayerPrefsKeys.S_UserName)))
            {
                playerName = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName));
            }
            NetPlayerName.Value = playerName; 
        }

        NetPlayerName.OnValueChanged += OnNameChanged;
        _txtPlayerName.SetText(NetPlayerName.Value.ToString());

        _btnPlayerReady.onClick.AddListener(OnBtnPlayerReadyClick);
    }

    public override void OnNetworkDespawn()
    {
        LobbyCoop lobby = Object.FindFirstObjectByType<LobbyCoop>();
        if (lobby != null)
        {
            lobby.UnregisterPlayer(this);
        }

        NetPlayerName.OnValueChanged -= OnNameChanged;
        _btnPlayerReady.onClick.RemoveListener(OnBtnPlayerReadyClick);
    }

    private void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        _txtPlayerName.SetText(newValue.ToString());
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

    private void OnBtnPlayerReadyClick()
    {
        if (IsOwner)
        {
            NetIsReady.Value = !NetIsReady.Value;
        }
    }
}