using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    public Transform attackPoint;    
    public float attackRange = 1.5f; 
    public LayerMask enemyLayers;    
    public float offsetDistance = 1.2f;

    [Header("Cấu hình phím đánh")]
    public KeyCode attackKey = KeyCode.Mouse0; // Mặc định là chuột trái cho Player 1

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

        // Nhấn đúng phím được gán mới đánh
        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
    }

    void Attack()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            StopAllCoroutines();
            StartCoroutine(HitStop(0.03f)); 

            Debug.Log(gameObject.name + " tiêu diệt: " + enemy.name);
            Destroy(enemy.gameObject);

            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) {
                ph.Heal(healthRegenPerKill);
            }
        }
    }

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.01f; // Tránh lỗi NaN khi để bằng 0
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