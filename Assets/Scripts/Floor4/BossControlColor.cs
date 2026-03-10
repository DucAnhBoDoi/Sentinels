using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BossController))]
public class BossControlColor : MonoBehaviour, IFlashLightInteract
{
    private enum BossColorState
    {
        NoColor,
        Color,
    }

    public event UnityAction OnColor;
    public event UnityAction<bool> OnNoColor;

    [SerializeField]
    private Color _inactiveColor;

    private BossController _boss;

    private FlashLightController _flashLight;

    private float _blowUpTimer;
    private float _flashTimer;
    private float _switchColorTimer;
    private bool _didBlowUp;
    private Color[] _palete;

    private BossColorState _state;

    public bool IsSpotted => _flashLight != null;

    private void Awake()
    {
        _boss = GetComponent<BossController>();
        _blowUpTimer = _boss.Stats.BlowUpTime;
        _flashTimer = _boss.Stats.FlashTime;
        _switchColorTimer = _boss.Stats.SwitchColorTime;
        _palete = _boss.Stats.BossColors;
    }

    private void Update()
    {
        switch (_state)
        {
            case BossColorState.NoColor:
                {
                    _switchColorTimer -= Time.deltaTime;
                    if (_switchColorTimer <= 0)
                    {
                        TransitionToColorState();
                    }
                }
                break;
            case BossColorState.Color:
                {
                    if (_flashLight == null || _flashLight.Light.color != _boss.BossBodySr.color)
                    {
                        _blowUpTimer -= Time.deltaTime;
                        _flashTimer = Mathf.Min(_boss.Stats.FlashTime, _flashTimer + Time.deltaTime);
                    }
                    else
                    {
                        _blowUpTimer += Mathf.Min(_boss.Stats.BlowUpTime, _blowUpTimer + Time.deltaTime / 2);
                        _flashTimer -= Time.deltaTime;
                    }

                    if (_flashTimer <= 0)
                    {
                        _state = BossColorState.NoColor;
                    }
                    else if (_blowUpTimer <= 0)
                    {
                        _state = BossColorState.NoColor;
                        _didBlowUp = true;
                    }

                    if (_state != BossColorState.Color)
                    {
                        TransitionToNoColorState();
                    }
                }
                break;
            default:
                break;
        }
    }

    public void TransitionToColorState()
    {
        _state = BossColorState.Color;
        _switchColorTimer = _boss.Stats.SwitchColorTime;
        _boss.BossBodySr.color = _palete[Random.Range(0, _palete.Length)];
        OnColor?.Invoke();
    }

    public void TransitionToNoColorState()
    {
        _flashTimer = _boss.Stats.FlashTime;
        _blowUpTimer = _boss.Stats.BlowUpTime;
        _boss.BossBodySr.color = _inactiveColor;
        OnNoColor?.Invoke(_didBlowUp);
        _didBlowUp = false;
    }

    public void OnFlashLightHit(FlashLightController flashLight)
    {
        _flashLight = flashLight;
    }

    public void OnFlashLightLeave(FlashLightController flashLight)
    {
        _flashLight = null;
    }
}
