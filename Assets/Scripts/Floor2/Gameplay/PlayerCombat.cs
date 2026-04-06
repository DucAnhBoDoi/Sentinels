using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;    
    public float attackRange = 1.5f; 
    public LayerMask enemyLayers;    

    [Header("Cấu hình phím đánh")]
    public KeyCode attackKey = KeyCode.Mouse0; 

    [Header("Hồi máu khi giết quái")]
    public float healthRegenPerKill = 6f; 

    [Header("Cấu hình Delay (Giây)")]
    public float attackDelay = 0.15f; // Chỉnh số này để khớp với lúc kiếm vung ra

    private Animator animator; 

    void Start()
    {
        animator = GetComponent<Animator>(); 
    }

    void Update()
    {
        if (Input.GetKeyDown(attackKey))
        {
            // Gọi Coroutine để xử lý đánh có độ trễ
            StartCoroutine(AttackWithDelay());
        }
    }

    IEnumerator AttackWithDelay()
    {
        // 1. Kích hoạt Animation ngay lập tức
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // 2. Chờ một khoảng thời gian ngắn để kiếm vung ra đúng vị trí
        yield return new WaitForSeconds(attackDelay);

        // 3. Sau khi chờ xong, mới thực hiện quét và giết quái
        PerformDamage();
    }

    void PerformDamage()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            ItemDropper dropper = enemy.GetComponent<ItemDropper>();
            if (dropper != null) dropper.DropRandomItem();

            StopAllCoroutines();
            StartCoroutine(HitStop(0.03f)); 

            Debug.Log("Chém chết quái sau khoảng trễ: " + enemy.name);
            Destroy(enemy.gameObject);

            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) ph.Heal(healthRegenPerKill);
        }
    }

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.01f; 
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f; 
    }

    void OnDrawGizmos()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red; 
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}