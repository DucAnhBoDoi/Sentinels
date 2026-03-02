// ══════════════════════════════════════════════════════
// FILE: PlayerMovement.cs
// ══════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public enum PlayerType { PlayerA, PlayerB }
    public PlayerType playerType;

    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    private Vector2 movement;

    [Header("Tầm tác động (Dành cho Player B)")]
    public float attackRange = 1.5f;
    public float interactRange = 1.5f;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!QuestPopupManager.isGameStarted) return;
        
        movement = Vector2.zero;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (playerType == PlayerType.PlayerA)
        {
            if (keyboard.wKey.isPressed) movement.y = 1f;
            else if (keyboard.sKey.isPressed) movement.y = -1f;
            if (keyboard.dKey.isPressed) movement.x = 1f;
            else if (keyboard.aKey.isPressed) movement.x = -1f;
        }
        else if (playerType == PlayerType.PlayerB)
        {
            if (keyboard.upArrowKey.isPressed) movement.y = 1f;
            else if (keyboard.downArrowKey.isPressed) movement.y = -1f;
            if (keyboard.rightArrowKey.isPressed) movement.x = 1f;
            else if (keyboard.leftArrowKey.isPressed) movement.x = -1f;

            if (keyboard.spaceKey.wasPressedThisFrame) PerformAttack();
            if (keyboard.eKey.wasPressedThisFrame) PerformRepair();

            // Broadcast trạng thái sửa điện cho TẤT CẢ robot mỗi frame
            // → Utility Score của Robot sẽ nhảy vọt khi B đang giữ E sửa điện
            bool repairing = keyboard.eKey.isPressed;
            foreach (var robot in UtilityRobotAI.allRobots)
                robot.isPlayerBRepairing = repairing;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void PerformAttack()
    {
        bool hit = false;
        foreach (Collider2D col in Physics2D.OverlapCircleAll(transform.position, attackRange))
        {
            var robot = col.GetComponent<UtilityRobotAI>();
            if (robot) { robot.TakeDamage(); hit = true; }
        }
        Debug.Log(hit ? "Player B: Đập nát quái!" : "Player B: Đánh hụt!");
    }

    void PerformRepair()
    {
            {
                node.FixNode();
                Debug.Log("Player B: Đã sửa xong Hộp Nối!");
                break;
            }
        }
    

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
