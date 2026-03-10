using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D), typeof(KnockBackManager))]
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

    public KnockBackManager KnockManager { get; private set; }

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
        Input = new();
        KnockManager = GetComponent<KnockBackManager>();
        KnockManager.KnockBackRecoverTime = Stats.KnockBackRecoverTime;
    }
}
