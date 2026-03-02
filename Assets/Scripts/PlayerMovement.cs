using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    private Vector2 movement;

    // Thêm lựa chọn kiểu điều khiển trong Inspector
    public enum ControlType { WASD, ArrowKeys }
    public ControlType controlType;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Đảm bảo nhân vật không bị rơi
        rb.freezeRotation = true; // Không cho nhân vật bị xoay tròn
    }

    void Update()
    {
        movement = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (controlType == ControlType.WASD)
        {
            // Cài đặt cho Player 1 (WASD)
            if (keyboard.wKey.isPressed) movement.y = 1f;
            else if (keyboard.sKey.isPressed) movement.y = -1f;

            if (keyboard.dKey.isPressed) movement.x = 1f;
            else if (keyboard.aKey.isPressed) movement.x = -1f;
        }
        else if (controlType == ControlType.ArrowKeys)
        {
            // Cài đặt cho Player 2 (Phím mũi tên)
            if (keyboard.upArrowKey.isPressed) movement.y = 1f;
            else if (keyboard.downArrowKey.isPressed) movement.y = -1f;

            if (keyboard.rightArrowKey.isPressed) movement.x = 1f;
            else if (keyboard.leftArrowKey.isPressed) movement.x = -1f;
        }
    }

    void FixedUpdate()
    {
        // Di chuyển mượt mà dựa trên moveSpeed
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}