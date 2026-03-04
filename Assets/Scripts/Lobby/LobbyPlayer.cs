using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<bool> NetIsReady = new(false, writePerm: NetworkVariableWritePermission.Owner);

    [SerializeField]
    private TMP_Text _txtPlayerName;

    [SerializeField]
    private Button _btnPlayerReady;
    private Image _imgBtnPlayerReady;

    private Color _colorReady;
    private Color _colorNotReady;

    private void Awake()
    {
        _imgBtnPlayerReady = _btnPlayerReady.GetComponent<Image>();
        _colorReady = Color.green;
        _colorNotReady = Color.white;
    }

    private void Start()
    {
        string playerName = "Default Player";
        if (PlayerPrefs.HasKey(nameof(PlayerPrefsKeys.S_UserName)))
        {
            playerName = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName));
        }

        _txtPlayerName.SetText(playerName);
        _btnPlayerReady.onClick.AddListener(OnBtnPlayerReadyClick);
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
        _btnPlayerReady.interactable = IsOwner;
    }

    private void OnBtnPlayerReadyClick()
    {
        if (IsOwner)
        {
            NetIsReady.Value = !NetIsReady.Value;
        }
    }
}
