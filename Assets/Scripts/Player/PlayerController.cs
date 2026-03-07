using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerControlDirection), typeof(PlayerControlFlashLight))]
[RequireComponent(typeof(PlayerIdle), typeof(PlayerMove))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Move,
    }

    [field: SerializeField]
    public PlayerStatsSO Stats { get; private set; }

    [field: SerializeField]
    public Light2D FlashLight { get; private set; }

    public Rigidbody2D Rb { get; private set; }

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
    }
}
