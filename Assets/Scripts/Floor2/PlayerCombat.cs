using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;    
    public float attackRange = 1.5f; 
    public LayerMask enemyLayers;    
    public float offsetDistance = 1.2f;

    [Header("Cấu hình phím đánh")]
    public KeyCode attackKey = KeyCode.Mouse0; 

    [Header("Hồi máu khi giết quái")]
    public float healthRegenPerKill = 15f; 

    void Update()
    {
        // Điều chỉnh hướng AttackPoint dựa trên phím di chuyển
        float moveInput = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput = 1;

        if (moveInput > 0) attackPoint.localPosition = new Vector3(offsetDistance, 0, 0);
        else if (moveInput < 0) attackPoint.localPosition = new Vector3(-offsetDistance, 0, 0);

        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
    }

    void Attack()
    {
        // Quét tất cả quái trong phạm vi vòng tròn
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // --- BẮT ĐẦU ĐOẠN SỬA ĐỂ RƠI ĐỒ ---
            // Thử tìm script ItemDropper trên con quái bị chém trúng
            ItemDropper dropper = enemy.GetComponent<ItemDropper>();
            if (dropper != null)
            {
                // Gọi hàm rơi đồ ngẫu nhiên
                dropper.DropRandomItem();
            }
            // --- KẾT THÚC ĐOẠN SỬA ---

            StopAllCoroutines();
            StartCoroutine(HitStop(0.03f)); 

            Debug.Log(gameObject.name + " tiêu diệt: " + enemy.name);
            
            // Xóa quái vật
            Destroy(enemy.gameObject);

            // Hồi máu cho người chơi
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) {
                ph.Heal(healthRegenPerKill);
            }
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