using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerMove : MonoBehaviour
{
    private InputSystem_Actions _input;

    private PlayerController _controller;

    private Vector2 _movement;

    private void Awake()
    {
        _input = new();
        _controller = GetComponent<PlayerController>();
    }

    void OnDestroy()
    {
        _input.Dispose();
    }

    void OnEnable()
    {
        _input.Enable();
    }

    void OnDisable()
    {
        _input.Disable();
    }

    private void Update()
    {
        _movement = _input.Player.Move.ReadValue<Vector2>();

        switch (_controller.State)
        {
            case PlayerController.PlayerState.Move:
                if (_controller.Rb.linearVelocity == Vector2.zero)
                {
                    _controller.State = PlayerController.PlayerState.Idle;
                }
                break;

            case PlayerController.PlayerState.Idle:
                {
                    if (_movement != Vector2.zero)
                    {
                        _controller.State = PlayerController.PlayerState.Move;
                    }
                }
                break;

            default:
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_controller.State != PlayerController.PlayerState.Move)
        {
            return;
        }

        _controller.Rb.linearVelocity = _movement.normalized * _controller.Stats.MovementSpeed;
    }
}
