// ══════════════════════════════════════════════════════
// FILE: PlayerMovement.cs (Di chuyển, Lộn, Lật mặt & Đánh)
// ══════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator anim;
    
    [Header("Cài đặt Chiến đấu")]
    public bool canAttack = true; // BẬT/TẮT quyền tấn công ở Inspector
    public float attackRange = 1.5f;
    public Vector2 actionOffset; // Dời tâm vòng tròn xuống chân

    private Vector2 movement;
    private bool isRolling = false;
    private float baseScaleX;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        baseScaleX = Mathf.Abs(transform.localScale.x); 
    }

    void Update()
    {
        if (!QuestPopupManager.isGameStarted || isRolling) return;
        
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // 1. DI CHUYỂN (W, A, S, D)
        movement = Vector2.zero;
        if (keyboard.wKey.isPressed) movement.y = 1f;
        else if (keyboard.sKey.isPressed) movement.y = -1f;
        if (keyboard.dKey.isPressed) movement.x = 1f;
        else if (keyboard.aKey.isPressed) movement.x = -1f;

        FlipCharacter();
        if (anim) anim.SetBool("isRunning", movement.sqrMagnitude > 0);

        // 2. LỘN (Space)
        if (keyboard.spaceKey.wasPressedThisFrame && movement != Vector2.zero)
        {
            PerformRoll();
        }

        // 3. TẤN CÔNG (Chuột trái) - Chỉ chạy nếu canAttack = true
        if (mouse.leftButton.wasPressedThisFrame && canAttack)
        {
            PerformAttack();
        }
    }

    void FixedUpdate()
    {
        if (!isRolling)
        {
            rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void FlipCharacter()
    {
        if (movement.x != 0)
        {
            float direction = Mathf.Sign(movement.x); 
            transform.localScale = new Vector3(baseScaleX * direction, transform.localScale.y, transform.localScale.z);
        }
    }

    void PerformRoll()
    {
        isRolling = true;
        if (anim) anim.SetTrigger("isRolling");
        rb.linearVelocity = movement.normalized * (moveSpeed * 1.5f);
        Invoke("FinishRoll", 0.5f);
    }

    void FinishRoll()
    {
        isRolling = false;
        rb.linearVelocity = Vector2.zero;
    }

    void PerformAttack()
    {
        if (anim) anim.SetTrigger("isAttacking");

        bool hit = false;
        
        // --- SỬA Ở ĐÂY: Tính toán lại Offset theo hướng mặt ---
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 centerPoint = (Vector2)transform.position + actualOffset; 
        // -----------------------------------------------------

        foreach (Collider2D col in Physics2D.OverlapCircleAll(centerPoint, attackRange))
        {
            var robot = col.GetComponent<UtilityRobotAI>();
            if (robot) { robot.TakeDamage(); hit = true; }
        }
        Debug.Log(hit ? gameObject.name + " đập trúng quái!" : gameObject.name + " đánh hụt!");
    }

    void OnDrawGizmosSelected()
    {
        // Vẽ Gizmos cũng phải nhân với hướng mặt để hiển thị đúng
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + actualOffset, attackRange);
    }
}