using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
    }

    void Update()
    {
        // Dùng Input System mới
        movement = Vector2.zero;
        
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                movement.y = 1f;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                movement.y = -1f;
                
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                movement.x = 1f;
            else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                movement.x = -1f;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}