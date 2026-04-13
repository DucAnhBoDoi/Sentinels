using UnityEngine;
using System.Collections.Generic;

public class BoidEnemy : MonoBehaviour
{
    [Header("Boid Settings")]
    public float speed = 3.0f;
    public float neighborRadius = 3.5f; 
    public float attackDamage = 5f;
    [Range(0, 5)] public float separationWeight = 2.5f; 
    [Range(0, 5)] public float cohesionWeight = 1.5f;   
    [Range(0, 5)] public float alignmentWeight = 1.2f;  
    public float targetWeight = 1.5f; // Lực hướng về Lõi

    [Header("Cấu hình tấn công")]
    // attackDistance phải LỚN HƠN bán kính Collider của cái Lõi
    public float attackDistance = 2.5f; 
    public Vector2 attackOffset; 
    public Vector2 damageRangeScale = new Vector2(1.5f, 0.8f); 
    public float attackCooldown = 1.5f; 
    private float lastAttackTime;

    [Header("Tham chiếu")]
    private Transform target;
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb; // Thêm Rigidbody để xử lý va chạm mượt hơn
    private Vector2 currentVelocity;
    private static List<BoidEnemy> allBoids = new List<BoidEnemy>();

    void Start() {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        GameObject core = GameObject.FindGameObjectWithTag("TheCore");
        if (core != null) target = core.transform;
        currentVelocity = Random.insideUnitCircle.normalized * speed;
    }

    void Update() {
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // --- LOGIC BAO VÂY RÌA ---
        if (distanceToTarget <= attackDistance) {
            // Khi đã vào tầm đánh (rìa lõi), dừng lực bầy đàn lại
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, Time.deltaTime * 5f);
            if (anim) anim.SetBool("isRunning", false);

            sr.flipX = (target.position.x > transform.position.x);

            if (Time.time >= lastAttackTime + attackCooldown) {
                if (anim) anim.SetTrigger("isAttacking");
                lastAttackTime = Time.time;
            }
            // Không thoát (return) hẳn để vẫn giữ Rigidbody hoạt động né nhau
        } else {
            HandleFlocking(); // Chỉ bay khi ở xa
        }
    }

    void HandleFlocking() {
        if (anim) anim.SetBool("isRunning", true);
        
        Vector2 separation = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        int neighborCount = 0;

        foreach (BoidEnemy boid in allBoids) {
            if (boid == this) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);
            if (dist < neighborRadius) {
                separation += (Vector2)(transform.position - boid.transform.position) / dist;
                cohesion += (Vector2)boid.transform.position;
                alignment += boid.currentVelocity;
                neighborCount++;
            }
        }

        Vector2 targetDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        sr.flipX = (targetDir.x > 0); 

        Vector2 finalFlockingDir = Vector2.zero;
        if (neighborCount > 0) {
            cohesion = (cohesion / neighborCount - (Vector2)transform.position).normalized;
            alignment = (alignment / neighborCount).normalized;
            finalFlockingDir = (separation.normalized * separationWeight) + 
                               (cohesion * cohesionWeight) + 
                               (alignment * alignmentWeight);
        }

        Vector2 combinedDir = (finalFlockingDir + targetDir * targetWeight).normalized;
        currentVelocity = Vector2.Lerp(currentVelocity, combinedDir * speed, Time.deltaTime * 3f);
    }

    void FixedUpdate() {
        // Dùng Rigidbody để di chuyển giúp quái không đi xuyên qua Collider của Lõi
        rb.linearVelocity = currentVelocity;
    }

    public void ExecuteBoidHit() {
        if (target == null) return;
        float facingDir = sr.flipX ? 1f : -1f; 
        Vector2 flippedOffset = new Vector2(attackOffset.x * facingDir, attackOffset.y);
        Vector2 attackCenter = (Vector2)transform.position + flippedOffset;

        Collider2D[] hitCore = Physics2D.OverlapBoxAll(attackCenter, damageRangeScale, 0f);
        foreach (Collider2D col in hitCore) {
            if (col.CompareTag("TheCore")) {
                LifeCore coreScript = col.GetComponent<LifeCore>();
                if (coreScript != null) {
                    coreScript.TakeDirectDamage(attackDamage);
                }
            }
        }
    }

    void OnDrawGizmos() {
        if(sr == null) sr = GetComponent<SpriteRenderer>();
        if(sr == null) return;
        float facingDir = sr.flipX ? 1f : -1f; 
        Gizmos.color = Color.red;
        Vector2 flippedOffset = new Vector2(attackOffset.x * facingDir, attackOffset.y);
        Vector3 gizmoCenter = transform.position + (Vector3)flippedOffset;
        Gizmos.DrawWireCube(gizmoCenter, (Vector3)damageRangeScale);
        
        // Vẽ thêm tầm dừng (Màu xanh) để bạn dễ căn chỉnh rìa
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }

    void OnEnable() { allBoids.Add(this); }
    void OnDisable() { allBoids.Remove(this); }
}