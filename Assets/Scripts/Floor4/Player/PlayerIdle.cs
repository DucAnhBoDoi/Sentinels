using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerIdle : MonoBehaviour
{
    private PlayerController _controller;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        switch (_controller.State)
        {
            case PlayerController.PlayerState.Move:
                {
                    if (!_controller.Input.Player.Move.IsPressed() &&
                            _controller.Rb.linearVelocity == Vector2.zero)
                    {
                        _controller.State = PlayerController.PlayerState.Idle;
                    }
                }
                break;

            case PlayerController.PlayerState.Idle:
            default:
                break;
        }
    }
}
