using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Cấu hình vùng đánh")]
    public Transform attackPoint;    
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;

    [Header("Cấu hình phím đánh")]
    public KeyCode attackKey = KeyCode.Mouse0;

    [Header("Hồi máu khi giết quái")]
    public float healthRegenPerKill = 6f;

    [Header("Cấu hình Delay")]
    public float attackDelay = 0.15f;

    private Animator animator;
    private bool isHitStopping = false;

    void Start() { animator = GetComponent<Animator>(); }

    void Update() {
        if (!QuestPopupManager.isGameStarted) return;
        if (Input.GetKeyDown(attackKey)) StartCoroutine(AttackWithDelay());
    }

    IEnumerator AttackWithDelay() {
        if (animator != null) animator.SetTrigger("isAttacking");
        yield return new WaitForSeconds(attackDelay);
        PerformDamage();
    }

    void PerformDamage() {
        if (attackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies) {
            // TÌM INTERFACE CHUNG
            IDamagable damageable = enemy.GetComponentInParent<IDamagable>();

            if (damageable != null) {
                // 1. Gây sát thương (Quái sẽ tự quyết định có chết hay rớt đồ không)
                damageable.TakeDamage();

                // 2. Hiệu ứng khựng hình (Fix lỗi đứng game)
                StartCoroutine(HitStop(0.05f));

                // 3. Hồi máu (Sử dụng script PlayerHP của bạn)
                PlayerHP ph = GetComponent<PlayerHP>();
                if (ph != null) ph.TakeDamage(-(int)healthRegenPerKill);
            }
        }
    }

    IEnumerator HitStop(float duration) {
        if (isHitStopping) yield break;
        isHitStopping = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f; // Trả về 1 để không bị chậm game vĩnh viễn
        isHitStopping = false;
    }

    void OnDrawGizmos() {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}