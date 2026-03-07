using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerMove : MonoBehaviour
{
    private PlayerController _controller;

    private Vector2 _movement;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        _movement = _controller.Input.Player.Move.ReadValue<Vector2>();

        switch (_controller.State)
        {
            case PlayerController.PlayerState.Idle:
                {
                    if (_movement != Vector2.zero)
                    {
                        _controller.State = PlayerController.PlayerState.Move;
                    }
                }
                break;

            case PlayerController.PlayerState.Move:
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
