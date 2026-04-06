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

    // Biến để lưu hướng mặt hiện tại
    private bool isFacingRight = true; 

    // 1. Khai báo biến Animator
    private Animator animator;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        // 2. Lấy component Animator từ nhân vật
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        movement = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (controlType == ControlType.WASD)
        {
            if (keyboard.wKey.isPressed) movement.y = 1f;
            else if (keyboard.sKey.isPressed) movement.y = -1f;

            if (keyboard.dKey.isPressed) movement.x = 1f;
            else if (keyboard.aKey.isPressed) movement.x = -1f;
        }
        else if (controlType == ControlType.ArrowKeys)
        {
            if (keyboard.upArrowKey.isPressed) movement.y = 1f;
            else if (keyboard.downArrowKey.isPressed) movement.y = -1f;

            if (keyboard.rightArrowKey.isPressed) movement.x = 1f;
            else if (keyboard.leftArrowKey.isPressed) movement.x = -1f;
        }

        // --- ĐOẠN CẬP NHẬT ANIMATION CHẠY ---
        if (animator != null)
        {
            // movement.magnitude sẽ bằng 0 nếu đứng yên, và > 0 nếu đang di chuyển
            // Gửi giá trị này vào tham số "Speed" trong cửa sổ Animator
            animator.SetFloat("Speed", movement.magnitude);
        }

        // --- ĐOẠN SỬA ĐỂ QUAY MẶT ---
        if (movement.x < 0 && isFacingRight)
        {
            Flip();
        }
        else if (movement.x > 0 && !isFacingRight)
        {
            Flip();
        }
    }

    void FixedUpdate()
    {
        // Di chuyển mượt mà dựa trên moveSpeed
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }
}