using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(KnockBackManager))]
[RequireComponent(typeof(PlayerControlDirection), typeof(PlayerControlFlashLight), typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerIdle), typeof(PlayerMove), typeof(PlayerKnockBack))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Move,
        KnockBack,
    }

    [field: SerializeField]
    public PlayerStatsSO Stats { get; private set; }

    [field: SerializeField]
    public Light2D FlashLight { get; private set; }

    public Rigidbody2D Rb { get; private set; }

    public SpriteRenderer Sr { get; private set; }

    public KnockBackManager KnockManager { get; private set; }

    public PlayerAttack PlayerAttackController { get; private set; }

    public InputSystem_Actions Input { get; private set; }

    [HideInInspector]
    public PlayerState State;

    void OnDestroy()
    {
        Input.Dispose();
    }

    void OnEnable()
    {
        Input.Enable();
    }

    void OnDisable()
    {
        Input.Disable();
    }

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Sr = GetComponent<SpriteRenderer>();
        Input = new();
        KnockManager = GetComponent<KnockBackManager>();
        KnockManager.KnockBackRecoverTime = Stats.KnockBackRecoverTime;
        PlayerAttackController = GetComponent<PlayerAttack>();
    }

    public void HurtAnimation()
    {
        Color cl = Color.white;
        DOTween.Kill(this);
        DOTween.Sequence(this)
            .Append(Sr.DOColor(Color.red, 0.2f))
            .Append(Sr.DOColor(cl, 0.2f))
            .OnKill(() => Sr.color = cl)
            .OnComplete(() => Sr.color = cl);
    }
}
