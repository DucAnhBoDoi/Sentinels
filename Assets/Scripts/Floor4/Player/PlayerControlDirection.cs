using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerControlDirection : MonoBehaviour
{
    private float _playerAngle;

    private void Update()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        Vector2 direction = mousePos - new Vector2(transform.position.x, transform.position.y);
        _playerAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(0, 0, _playerAngle);
    }
}
