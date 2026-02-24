using UnityEngine;

public class SimpleEnemy : MonoBehaviour
{
    public float speed = 0.5f;        // Tốc độ đi chậm từ rìa vào
    public float attackRange = 1.2f;  // Khoảng cách đủ gần để "nổ"
    public float damageToCore = 15f;  // Lượng máu Lõi bị mất ngay lập tức khi quái chết
    
    private Transform targetCore;
    private LifeCore coreScript;

    void Start()
    {
        // Tìm cái Lõi bằng Tag
        GameObject core = GameObject.FindGameObjectWithTag("TheCore");
        if (core != null) 
        {
            targetCore = core.transform;
            coreScript = core.GetComponent<LifeCore>();
        }
    }

    void Update()
    {
        if (targetCore == null) return;

        // 1. Luôn di chuyển về phía Lõi
        transform.position = Vector2.MoveTowards(transform.position, targetCore.position, speed * Time.deltaTime);

        // 2. Tính khoảng cách tới Lõi
        float distance = Vector2.Distance(transform.position, targetCore.position);

        // 3. Nếu đã đến đủ gần
        if (distance <= attackRange)
        {
            AttackAndDie();
        }
    }

    void AttackAndDie()
    {
        if (coreScript != null)
        {
            // Trừ máu lõi ngay lập tức
            coreScript.TakeDirectDamage(damageToCore);
            
            // Kích hoạt trạng thái UnderAttack để người chơi biết cần vào nạp máu
            coreScript.isUnderAttack = true; 
        }

        // Tạo hiệu ứng (nếu có) và xóa con quái
        Debug.Log("Quái đã áp sát Lõi và tự hủy!");
        Destroy(gameObject);
    }
}