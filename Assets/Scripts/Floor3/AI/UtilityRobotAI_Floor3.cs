using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class UtilityRobotAI_Floor3 : MonoBehaviour, IDamagable 
{
    [Header("Targets")]
    public Transform playerA;
    public Transform playerB;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolSpeed = 1.5f;
    public float detectionRadius = 7f;
    public float stoppingDistance = 1.2f; 

    [Header("Patrol")]
    public float patrolRadius = 4f;
    public float stuckTimeLimit = 3f;

    [Header("Steering")]
    public int scanDirections = 12;
    public float scanStepSize = 1.5f;
    public float robotRadius = 0.4f;

    [Header("Obstacle")]
    public LayerMask obstacleLayer;

    // ── HỆ THỐNG TẤN CÔNG ──
    [Header("Attack Settings")]
    public float attackDamage = 1f; 
    public float attackCooldown = 1.5f;

    public static List<UtilityRobotAI_Floor3> allRobots = new List<UtilityRobotAI_Floor3>();

    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private Vector2 startPos;
    private Vector2 patrolTarget;

    private float stuckTimer = 0f;
    private float currentCooldown = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true; // Chống lăn lộn

        allRobots.Add(this);

        startPos = transform.position;
        PickNewPatrolPoint();

        if (playerA == null) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (playerB == null) playerB = GameObject.Find("Player_B_Mechanic")?.transform;
    }

    void OnDestroy()
    {
        allRobots.Remove(this);
    }

    void FixedUpdate() 
    {
        // Giữ quái đứng im khi chưa bấm Start Mission (Bảo vệ lúc đang đọc Quest)
        if (!Scripts.Floor3.UI.TopicSelectionUI.hasStartedMission) return;

        if (currentCooldown > 0)
            currentCooldown -= Time.fixedDeltaTime;

        Transform target = FindNearestPlayer();

        if (target != null)
        {
            ExecuteMovement(target);
            TickAttack(); // Gọi lệnh quét Radar để cắn
        }
        else
        {
            ExecuteMovement(null); 
        }
    }

    // ── LOGIC CẮN BẰNG RADAR (CHUẨN 100%, KHÔNG TRƯỢT) ──
    void TickAttack()
    {
        if (currentCooldown > 0) return; 

        // Tạo vòng tròn quét. Bán kính to hơn stoppingDistance 0.3m để chắc chắn bao trùm được Player
        float attackRadius = stoppingDistance + 0.3f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);

        foreach (Collider2D hit in hits)
        {
            PlayerHP hp = hit.GetComponent<PlayerHP>();
            if (hp != null && !hp.IsDead)
            {
                hp.TakeDamage(attackDamage);
                currentCooldown = attackCooldown; 
                return; // Cắn được 1 người là dừng chờ hồi chiêu, không cắn lan
            }
        }
    }

    Transform FindNearestPlayer()
    {
        float distA = playerA ? Vector2.Distance(transform.position, playerA.position) : float.MaxValue;
        float distB = playerB ? Vector2.Distance(transform.position, playerB.position) : float.MaxValue;

        if (distA > detectionRadius && distB > detectionRadius)
            return null;

        return distA < distB ? playerA : playerB;
    }

    void ExecuteMovement(Transform target)
    {
        Vector2 currentPos = transform.position;
        Vector2 bestDir = Vector2.zero;
        float currentSpeed = patrolSpeed;

        if (target == null)
        {
            bestDir = (patrolTarget - currentPos).normalized;
            stuckTimer += Time.fixedDeltaTime;

            if (Vector2.Distance(currentPos, patrolTarget) < 0.2f ||
                stuckTimer > stuckTimeLimit ||
                Physics2D.CircleCast(currentPos, robotRadius, bestDir, 0.5f, obstacleLayer))
            {
                PickNewPatrolPoint();
            }
        }
        else
        {
            currentSpeed = moveSpeed;
            float distToTarget = Vector2.Distance(currentPos, target.position);

            if (distToTarget <= stoppingDistance)
            {
                bestDir = Vector2.zero; // Tới gần thì đứng lại chuẩn bị cắn
            }
            else
            {
                Vector2 idealDir = ((Vector2)target.position - currentPos).normalized;
                float highestScore = float.MinValue;

                for (int i = 0; i < scanDirections; i++)
                {
                    float angle = (360f / scanDirections) * i * Mathf.Deg2Rad;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float score = Vector2.Dot(dir, idealDir);

                    RaycastHit2D hit = Physics2D.CircleCast(currentPos, robotRadius, dir, scanStepSize, obstacleLayer);

                    if (hit.collider != null) score -= 10f;

                    if (score > highestScore)
                    {
                        highestScore = score;
                        bestDir = dir;
                    }
                }
            }
        }

        if (bestDir != Vector2.zero)
        {
            rb.linearVelocity = bestDir.normalized * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // XỬ LÝ LẬT MẶT
        if (target != null)
        {
            if (target.position.x < transform.position.x - 0.1f) sr.flipX = true;
            else if (target.position.x > transform.position.x + 0.1f) sr.flipX = false;
        }
        else if (Mathf.Abs(bestDir.x) > 0.1f)
        {
            sr.flipX = bestDir.x < 0; 
        }
    }

    void PickNewPatrolPoint()
    {
        patrolTarget = startPos + Random.insideUnitCircle * patrolRadius;
        stuckTimer = 0f;
    }

    public void TakeDamage()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            Application.isPlaying ? startPos : (Vector2)transform.position,
            patrolRadius
        );

        // Vẽ cái "Radar cắn" màu Vàng cho anh dễ nhìn trong Scene
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance + 0.3f);
    }
}