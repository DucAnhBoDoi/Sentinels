// ══════════════════════════════════════════════════════
// FILE: PlayerMovement.cs (Di chuyển, Lộn, Lật mặt & Đánh)
// Dùng cho mọi máy, mọi Tầng.
// ══════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    public float moveSpeed = 5f;
    public bool useQuestSystem = true; // Tầng 3: HÃY BỎ TICK Ô NÀY TRONG INSPECTOR

    [Header("Thành phần hỗ trợ")]
    public Rigidbody2D rb;
    public Animator anim;

    [Header("Cấu hình Chiến đấu")]
    public bool canAttack = true;
    public float attackRange = 1.5f;
    public Vector2 actionOffset;
    public LayerMask enemyLayer;

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
        // Kiểm tra xem có đang bị kẹt bởi bảng Quest không (chỉ dùng nếu useQuestSystem = true)
        if (useQuestSystem && !QuestPopupManager.isGameStarted) return;

        // Nếu đang lộn thì không cho nhận thêm input di chuyển
        if (isRolling) return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // 1. NHẬN INPUT DI CHUYỂN (WASD cho cả 2 máy)
        movement = Vector2.zero;
        if (keyboard.wKey.isPressed) movement.y = 1f;
        else if (keyboard.sKey.isPressed) movement.y = -1f;
        if (keyboard.dKey.isPressed) movement.x = 1f;
        else if (keyboard.aKey.isPressed) movement.x = -1f;

        FlipCharacter();

        // Cập nhật Animation chạy
        if (anim) anim.SetBool("isRunning", movement.sqrMagnitude > 0);

        // 2. LỘN (Phím Space)
        if (keyboard.spaceKey.wasPressedThisFrame && movement != Vector2.zero)
        {
            PerformRoll();
        }

        // 3. TẤN CÔNG (Chuột trái)
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

        // Unity 6 dùng linearVelocity, các bản cũ dùng velocity
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

    }

    public void ExecutePlayerHit()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 centerPoint = (Vector2)transform.position + actualOffset;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(centerPoint, attackRange, enemyLayer);

        foreach (Collider2D col in hitEnemies)
        {
            var skeleton = col.GetComponentInParent<SkeletonAI>();
            if (skeleton) skeleton.TakeDamage();
        }
    }

    void OnDrawGizmosSelected()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + actualOffset, attackRange);
    }
}