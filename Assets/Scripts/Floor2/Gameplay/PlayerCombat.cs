// ══════════════════════════════════════════════════════════════
// FILE: PlayerCombat.cs
// PURPOSE: Xử lý sát thương dựa trên Attack Point (Transform)
// ══════════════════════════════════════════════════════════════

using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Cấu hình vùng đánh")]
    public Transform attackPoint;    // Kéo GameObject con vào đây
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;

    [Header("Cấu hình phím đánh")]
    public KeyCode attackKey = KeyCode.Mouse0;

    [Header("Hồi máu khi giết quái")]
    public float healthRegenPerKill = 6f;

    [Header("Cấu hình Delay (Giây)")]
    public float attackDelay = 0.15f;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Nhận lệnh đánh từ phím cấu hình
        if (Input.GetKeyDown(attackKey))
        {
            StartCoroutine(AttackWithDelay());
        }
    }

    IEnumerator AttackWithDelay()
    {
        // 1. Kích hoạt Animation
        if (animator != null)
        {
            // Trigger phải khớp với tên trong Animator của bạn
            animator.SetTrigger("isAttacking");
        }

        // 2. Chờ kiếm vung ra (Delay)
        yield return new WaitForSeconds(attackDelay);

        // 3. Thực hiện gây sát thương
        PerformDamage();
    }

    void PerformDamage()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning("Chưa kéo Attack Point vào Inspector!");
            return;
        }

        // Sử dụng vị trí chính xác của attackPoint làm tâm vòng tròn
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Rơi vật phẩm
            ItemDropper dropper = enemy.GetComponent<ItemDropper>();
            if (dropper != null) dropper.DropRandomItem();

            // Hiệu ứng khựng hình (Hitstop)
            StartCoroutine(HitStop(0.03f));

            Debug.Log("Đã tiêu diệt: " + enemy.name);

            // Kiểm tra Robot (Tầng 1) hoặc quái thường (Tầng khác)
            var skeleton = enemy.GetComponent<SkeletonAI>();
            if (skeleton != null)
            {
                skeleton.TakeDamage();
            }
            else
            {
                Destroy(enemy.gameObject);
            }

            // Hồi máu
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) ph.Heal(healthRegenPerKill);
        }
    }

    IEnumerator HitStop(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.01f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }

    // Vẽ vùng đánh màu đỏ trong Scene để dễ căn chỉnh
    void OnDrawGizmos()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}