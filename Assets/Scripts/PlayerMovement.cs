// ══════════════════════════════════════════════════════
// FILE: PlayerMovement.cs (Di chuyển, Lộn, Lật mặt & Đánh)
// Dùng cho mọi máy, mọi Tầng.
// ══════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Cấu hình di chuyển")]
    public float moveSpeed = 5f;
    public bool useQuestSystem = true;

    [Header("Thành phần hỗ trợ")]
    public Rigidbody2D rb;
    public Animator anim;
    private Unity.Netcode.Components.NetworkAnimator netAnim;

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

        netAnim = GetComponent<Unity.Netcode.Components.NetworkAnimator>();

        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        baseScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Update()
    {
        if (!IsOwner) return;
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
        // Dùng netAnim cho đồng bộ mạng, nếu không có thì dùng anim thường
        if (movement.sqrMagnitude > 0)
        {
            if (netAnim) netAnim.Animator.SetBool("isRunning", true);
            else if (anim) anim.SetBool("isRunning", true);
        }
        else
        {
            if (netAnim) netAnim.Animator.SetBool("isRunning", false);
            else if (anim) anim.SetBool("isRunning", false);
        }

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
        if (!IsOwner) return;

        if (!isRolling)
        {
            if (movement != Vector2.zero)
            {
                rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
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

        if (netAnim) netAnim.SetTrigger("isRolling");
        else if (anim) anim.SetTrigger("isRolling"); // Phòng hờ nếu test offline

        rb.linearVelocity = movement.normalized * (moveSpeed * 1.5f);
        Invoke("FinishRoll", 0.5f);
    }

    void FinishRoll()
    {
        isRolling = false;
        rb.linearVelocity = Vector2.zero;
    }

    // ĐÃ XÓA HÀM TRÙNG LẶP, CHỈ GIỮ LẠI HÀM NÀY
    void PerformAttack()
    {
        if (netAnim) netAnim.SetTrigger("isAttacking");
        else if (anim) anim.SetTrigger("isAttacking");
    }

    public void ExecutePlayerHit()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 centerPoint = (Vector2)transform.position + actualOffset;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(centerPoint, attackRange, enemyLayer);

        foreach (Collider2D col in hitEnemies)
        {
            var skeleton = col.GetComponentInParent<IDamagable>();
            if (skeleton != null) skeleton.TakeDamage();
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