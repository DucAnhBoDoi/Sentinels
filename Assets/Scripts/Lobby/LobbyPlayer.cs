using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<bool> IsReady = new(false, writePerm: NetworkVariableWritePermission.Owner);

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
        _btnPlayerReady.onClick.AddListener(OnBtnPlayerReadyClick);
    }

    private void Update()
    {
        if (IsReady.Value && _imgBtnPlayerReady.color != _colorReady)
        {
            _imgBtnPlayerReady.color = _colorReady;
        }
        else if (!IsReady.Value && _imgBtnPlayerReady.color != _colorNotReady)
        {
            _imgBtnPlayerReady.color = _colorNotReady;
        }
    }

    private void OnBtnPlayerReadyClick()
    {
        IsReady.Value = !IsReady.Value;
    }

    public override void OnGainedOwnership()
    {
        _btnPlayerReady.interactable = true;
    }

    public override void OnLostOwnership()
    {
        _btnPlayerReady.interactable = false;
    }
}
