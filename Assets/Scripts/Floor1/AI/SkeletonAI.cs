using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))] 
[RequireComponent(typeof(Animator))] // Bắt buộc phải có Animator
public class SkeletonAI : MonoBehaviour
{
    [Header("Tham chiếu")]
    public Transform playerA;
    public Transform playerB;
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    [Header("Chỉ số AI")]
    public float moveSpeed = 2.5f;
    public float patrolSpeed = 1.2f;
    public float detectionRadius = 8f;
    public float attackRange = 1.2f; // Khoảng cách để vung kiếm
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Trạng thái")]
    public int health = 3;
    private bool isDead = false;
    private bool isAggroed = false;
    public bool isPlayerBRepairing = false;

    [Header("Steering (Né vật cản)")]
    public float patrolRadius = 4f;
    public LayerMask obstacleLayer;
    public int scanDirections = 16;
    public float scanStepSize = 1.5f;
    public float robotRadius = 0.4f;
    public float distanceWeight = 5f, dangerWeight = 2f, momentumWeight = 8f;

    public static List<SkeletonAI> allRobots = new List<SkeletonAI>();
    private Vector2 startPos, patrolTarget;
    private float stuckTimer = 0f;

    void Start() 
    { 
        sr = GetComponent<SpriteRenderer>(); 
        rb = GetComponent<Rigidbody2D>(); 
        anim = GetComponent<Animator>();
        
        allRobots.Add(this); 
        startPos = transform.position; 
        PickNewPatrolPoint(); 

        // Tự động tìm Player nếu chưa kéo vào
        if (playerA == null) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (playerB == null) playerB = GameObject.Find("Player_B_Mechanic")?.transform;
    }

    void OnDestroy() => allRobots.Remove(this);

    void Update() 
    {
        if (isDead) return;

        Transform target = DecideTarget();
        
        // Kiểm tra khoảng cách tấn công
        if (target != null && Vector2.Distance(transform.position, target.position) <= attackRange)
        {
            TryAttack();
            rb.linearVelocity = Vector2.zero; // Dừng lại khi đánh
            anim.SetBool("isRunning", false);
        }
        else
        {
            ExecuteContextSteering(target);
        }
    }

    void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            anim.SetTrigger("isAttacking");
            lastAttackTime = Time.time;
            // Ở đây bạn có thể thêm code gây sát thương cho Player
        }
    }

    Transform DecideTarget()
    {
        if (playerA == null && playerB == null) return null;

        float distA = playerA ? Vector2.Distance(transform.position, playerA.position) : float.MaxValue;
        float distB = playerB ? Vector2.Distance(transform.position, playerB.position) : float.MaxValue;

        if (!isAggroed && (distA <= detectionRadius || distB <= detectionRadius)) isAggroed = true; 
        if (!isAggroed) return null; 

        float scoreB = (playerB) ? (distanceWeight / Mathf.Max(distB, 0.1f)) + (isPlayerBRepairing ? 50f : 0f) : 0f;
        float scoreA = (playerA) ? (distanceWeight / Mathf.Max(distA, 0.1f)) : 0f;

        return scoreB > scoreA ? playerB : playerA;
    }

    void ExecuteContextSteering(Transform target)
    {
        Vector2 currentPos = transform.position;
        Vector2 bestDir = Vector2.zero;
        float currentSpeed = (target == null) ? patrolSpeed : moveSpeed;
        Vector2 currentVelocityDir = rb.linearVelocity.normalized;

        if (target == null) // ĐI TUẦN
        {
            bestDir = (patrolTarget - currentPos).normalized;
            if (Vector2.Distance(currentPos, patrolTarget) < 0.2f || 
                Physics2D.CircleCast(currentPos, robotRadius, bestDir, 0.5f, obstacleLayer))
            {
                PickNewPatrolPoint();
            }
        }
        else // TRUY SÁT
        {
            float highestScore = float.MinValue;
            Vector2 idealDir = ((Vector2)target.position - currentPos).normalized;

            for (int i = 0; i < scanDirections; i++)
            {
                float rad = (360f / scanDirections * i) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                float score = Vector2.Dot(dir, idealDir) * distanceWeight;

                if (currentVelocityDir != Vector2.zero) score += Vector2.Dot(dir, currentVelocityDir) * momentumWeight;
                if (InfluenceMap.Instance) score -= InfluenceMap.Instance.GetDangerValue(currentPos + dir * scanStepSize) * dangerWeight;
                if (Physics2D.CircleCast(currentPos, robotRadius, dir, scanStepSize, obstacleLayer)) score -= 10000f;

                if (score > highestScore) { highestScore = score; bestDir = dir; }
            }
        }

        if (bestDir != Vector2.zero)
        {
            rb.linearVelocity = bestDir.normalized * currentSpeed; 
            sr.flipX = bestDir.x < 0; 
            anim.SetBool("isRunning", true); // Bật anim chạy
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("isRunning", false); // Tắt anim chạy
        }
    }

    void PickNewPatrolPoint() { patrolTarget = startPos + Random.insideUnitCircle * patrolRadius; }

    public void TakeDamage()
    {
        if (isDead) return;

        health--;
        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else
        {
            anim.SetTrigger("isHurt");
        }
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("isDead"); // Kích hoạt animation chết
        
        // Tắt va chạm để không cản đường Player
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false; 

        yield return new WaitForSeconds(2f); // Đợi 2 giây rồi biến mất
        Destroy(gameObject);
    }
}