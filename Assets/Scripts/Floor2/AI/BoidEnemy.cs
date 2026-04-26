using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Animator))]
public class BoidEnemy : MonoBehaviour, IDamagable 
{
    [Header("Boid Settings")]
    public float speed = 3.0f;
    public float neighborRadius = 3.5f;
    public float attackDamage = 5f;

    [Range(0, 10)] public float separationWeight = 8.0f; // 🔥 Tăng mạnh để quái dạt ra
    [Range(0, 5)] public float cohesionWeight = 0.5f;    // 🔥 Giảm cực thấp để tránh tụ đàn
    [Range(0, 5)] public float alignmentWeight = 1.0f;
    public float targetWeight = 2.0f;

    [Header("Anti-Clump")]
    public float separationDistance = 1.3f;     
    public float extraSeparationForce = 6.0f;   

    [Header("Cấu hình sinh tồn")]
    public int health = 3;
    private bool isDead = false;

    [Header("Cấu hình tấn công")]
    public float attackDistance = 1.2f; // 🔥 Tăng nhẹ để quái không phải đứng quá sát
    public Vector2 attackOffset;
    public Vector2 damageRangeScale = new Vector2(1.5f, 1.5f); 
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Tham chiếu")]
    private Transform target;
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Vector2 currentVelocity;
    private static List<BoidEnemy> allBoids = new List<BoidEnemy>();

    void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        GameObject core = GameObject.FindGameObjectWithTag("TheCore");
        if (core != null) target = core.transform;

        // Random hướng ban đầu cực mạnh để phá đội hình
        currentVelocity = Random.insideUnitCircle.normalized * speed;
    }

    public void TakeDamage()
    {
        if (isDead) return;
        health--;
        if (health <= 0) Die();
        else if (anim) anim.SetTrigger("isHurt");
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (anim) anim.SetTrigger("isDead");

        ItemDropper dropper = GetComponent<ItemDropper>();
        if (dropper != null) dropper.DropRandomItem();

        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 1.2f);
    }

    void Update()
    {
        if (isDead || target == null) return;

        // Tính khoảng cách đến lõi
        float distToTarget = Vector2.Distance(transform.position, target.position);
        Collider2D targetCol = target.GetComponent<Collider2D>();
        Vector2 targetCenter = target.position;

        if (targetCol != null)
        {
            targetCenter = targetCol.bounds.center;
            Vector2 closestPoint = targetCol.ClosestPoint(transform.position);
            distToTarget = Vector2.Distance(transform.position, closestPoint);
        }

        if (distToTarget <= attackDistance)
        {
            // 🔥 KHI ĐANG ĐÁNH: Vẫn phải giữ khoảng cách với con bên cạnh
            Vector2 escapeDir = CalculatePureSeparation();
            currentVelocity = Vector2.Lerp(currentVelocity, escapeDir * speed * 0.5f, Time.deltaTime * 5f);

            if (anim) anim.SetBool("isRunning", false);
            sr.flipX = (targetCenter.x > transform.position.x);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (anim) anim.SetTrigger("isAttacking");
                lastAttackTime = Time.time;
            }
        }
        else 
        { 
            HandleFlocking(targetCenter); 
        }
    }

    // Hàm chỉ tính toán lực tách để dùng khi đang đứng đánh
    Vector2 CalculatePureSeparation()
    {
        Vector2 sep = Vector2.zero;
        foreach (BoidEnemy boid in allBoids)
        {
            if (boid == this || boid == null) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);
            if (dist < separationDistance)
            {
                sep += ((Vector2)transform.position - (Vector2)boid.transform.position).normalized / dist;
            }
        }
        return sep.normalized;
    }

    void HandleFlocking(Vector2 targetCenter)
    {
        if (anim) anim.SetBool("isRunning", true);

        Vector2 separation = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        int neighborCount = 0;

        foreach (BoidEnemy boid in allBoids)
        {
            if (boid == this || boid == null) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);

            if (dist < neighborRadius)
            {
                Vector2 diff = ((Vector2)transform.position - (Vector2)boid.transform.position).normalized;
                
                // 🔥 Lực tách tỷ lệ nghịch với khoảng cách (càng gần đẩy càng mạnh)
                float force = (dist < separationDistance) ? extraSeparationForce : 1.0f;
                separation += diff * (force / Mathf.Max(dist, 0.1f));

                cohesion += (Vector2)boid.transform.position;
                alignment += boid.currentVelocity;
                neighborCount++;
            }
        }

        Vector2 targetDir = (targetCenter - (Vector2)transform.position).normalized;
        
        Vector2 flockingDir = Vector2.zero;
        if (neighborCount > 0)
        {
            cohesion = ((cohesion / neighborCount) - (Vector2)transform.position).normalized;
            alignment = (alignment / neighborCount).normalized;
            flockingDir = (separation * separationWeight) + (cohesion * cohesionWeight) + (alignment * alignmentWeight);
        }

        Vector2 combinedDir = flockingDir + targetDir * targetWeight;
        combinedDir += Random.insideUnitCircle * 0.2f; // Tăng độ nhiễu

        currentVelocity = Vector2.Lerp(currentVelocity, combinedDir.normalized * speed, Time.deltaTime * 4f);
        if (currentVelocity.magnitude > 0.1f) sr.flipX = currentVelocity.x > 0;
    }

    void FixedUpdate()
    {
        if (!isDead) rb.linearVelocity = currentVelocity;
    }

    // 🔥 SỬA LỖI ĐÁNH KHÔNG MẤT MÁU TẠI ĐÂY
    public void ExecuteBoidHit()
    {
        if (target == null || isDead) return;

        // Xác định hướng: Nếu đang nhìn phải (flipX true) thì đánh sang phải (+), ngược lại sang trái (-)
        float lookDir = sr.flipX ? 1f : -1f;

        // Tính toán vị trí tâm đòn đánh (Attack Hitbox)
        // Lưu ý: Không dùng Mathf.Abs ở đây để đảm bảo offset đi đúng hướng nhìn
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * lookDir, attackOffset.y);

        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(attackCenter, damageRangeScale, 0f);

        foreach (Collider2D col in hitObjects)
        {
            if (col.CompareTag("TheCore"))
            {
                LifeCore coreScript = col.GetComponent<LifeCore>();
                if (coreScript != null)
                {
                    coreScript.TakeDirectDamage(attackDamage);
                }
            }
        }
    }

    // Vẽ vùng đánh để bạn dễ căn chỉnh trong Scene
    void OnDrawGizmosSelected()
    {
        float lookDir = (sr != null && sr.flipX) ? 1f : -1f;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * lookDir, attackOffset.y);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackCenter, damageRangeScale);
    }

    void OnEnable() { if(!allBoids.Contains(this)) allBoids.Add(this); }
    void OnDisable() { allBoids.Remove(this); }
}