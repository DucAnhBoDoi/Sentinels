using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Animator))]
public class BoidEnemy : MonoBehaviour, IDamagable 
{
    [Header("Boid Settings")]
    public float speed = 3.0f;
    public float neighborRadius = 3.5f;
    public float attackDamage = 5f;
    [Range(0, 5)] public float separationWeight = 2.5f;
    [Range(0, 5)] public float cohesionWeight = 1.5f;
    [Range(0, 5)] public float alignmentWeight = 1.2f;
    public float targetWeight = 1.5f;

    [Header("Cấu hình sinh tồn")]
    public int health = 3;
    private bool isDead = false;

    [Header("Cấu hình tấn công")]
    public float attackDistance = 1.0f; // CHÚ Ý: ĐÃ GIẢM MẶC ĐỊNH XUỐNG 1.0
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

        // === BẮT ĐẦU SỬA LỖI: TÍNH KHOẢNG CÁCH TỚI VIỀN COLLIDER ===
        float distanceToEdge = 0f;
        Vector2 targetCenter = target.position; 
        Collider2D targetCol = target.GetComponent<Collider2D>();

        if (targetCol != null)
        {
            targetCenter = targetCol.bounds.center; // Lấy tâm vật lý giữa Collider
            
            // Tìm điểm gần con quái nhất trên viền của cái Lõi
            Vector2 closestPoint = targetCol.ClosestPoint(transform.position);
            distanceToEdge = Vector2.Distance(transform.position, closestPoint);
        }
        else
        {
            distanceToEdge = Vector2.Distance(transform.position, target.position);
        }

        // Đánh giá dựa trên khoảng cách tới viền
        if (distanceToEdge <= attackDistance)
        {
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, Time.deltaTime * 5f);
            if (anim) anim.SetBool("isRunning", false);
            
            // Nhìn về tâm vật lý của Lõi thay vì gốc tọa độ
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

    // Truyền thêm targetCenter để bầy quái bu về đúng tâm
    void HandleFlocking(Vector2 targetCenter)
    {
        if (anim) anim.SetBool("isRunning", true);
        Vector2 separation = Vector2.zero, cohesion = Vector2.zero, alignment = Vector2.zero;
        int neighborCount = 0;

        foreach (BoidEnemy boid in allBoids)
        {
            if (boid == this || boid == null) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);
            if (dist < neighborRadius)
            {
                separation += (Vector2)(transform.position - boid.transform.position) / dist;
                cohesion += (Vector2)boid.transform.position;
                alignment += boid.currentVelocity;
                neighborCount++;
            }
        }

        // Hướng bay về tâm của Lõi
        Vector2 targetDir = (targetCenter - (Vector2)transform.position).normalized;
        sr.flipX = (targetDir.x > 0);

        Vector2 finalFlockingDir = Vector2.zero;
        if (neighborCount > 0)
        {
            cohesion = (cohesion / neighborCount - (Vector2)transform.position).normalized;
            alignment = (alignment / neighborCount).normalized;
            finalFlockingDir = (separation.normalized * separationWeight) + (cohesion * cohesionWeight) + (alignment * alignmentWeight);
        }

        Vector2 combinedDir = (finalFlockingDir + targetDir * targetWeight).normalized;
        currentVelocity = Vector2.Lerp(currentVelocity, combinedDir * speed, Time.deltaTime * 3f);
    }

    void FixedUpdate() { if (!isDead) rb.linearVelocity = currentVelocity; }

    public void ExecuteBoidHit()
    {
        if (target == null) return;
        
        float dir = sr.flipX ? 1f : -1f;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(Mathf.Abs(attackOffset.x) * dir, attackOffset.y);
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(attackCenter, damageRangeScale, 0f);

        foreach (Collider2D col in hitObjects)
        {
            if (col.CompareTag("TheCore"))
            {
                LifeCore coreScript = col.GetComponent<LifeCore>();
                if (coreScript != null)
                {
                    coreScript.TakeDirectDamage(attackDamage);
                    Debug.Log("<color=red>Lõi bị tấn công!</color>");
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        float dir = sr.flipX ? 1f : -1f;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(Mathf.Abs(attackOffset.x) * dir, attackOffset.y);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackCenter, (Vector3)damageRangeScale);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
    
    void OnEnable() { allBoids.Add(this); }
    void OnDisable() { allBoids.Remove(this); }
}