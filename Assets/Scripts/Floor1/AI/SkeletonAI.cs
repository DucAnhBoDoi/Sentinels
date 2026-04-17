using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Animator))]
public class SkeletonAI : MonoBehaviour, IDamagable
{
    [Header("Tham chiếu")]
    public Transform playerA;
    public Transform playerB;
    public HealthBar healthBar;
    public LayerMask playerLayer; // MỚI: Dùng để chỉ chém vào Hitbox Player

    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    [Header("Chỉ số AI")]
    public float moveSpeed = 2.5f;
    public float patrolSpeed = 1.2f;
    public float detectionRadius = 8f;
    public float attackRange = 1.2f;
    public Vector2 actionOffset;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Trạng thái")]
    public int health = 3;
    private int maxHealth;
    private bool isDead = false;
    private bool isAggroed = false;
    public bool isPlayerBRepairing = false;

    [Header("Steering")]
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

        if (playerA == null) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (playerB == null) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        maxHealth = health;
        if (healthBar) healthBar.UpdateBar(health, maxHealth);
    }

    void OnDestroy() => allRobots.Remove(this);

    void Update()
    {
        if (isDead) return;

        Transform target = DecideTarget();

        // --- CẬP NHẬT: CHECK NÉ ĐÈN TRƯỚC KHI ĐÁNH ---
        float currentDanger = (InfluenceMap.Instance != null) ? InfluenceMap.Instance.GetDangerValue(transform.position) : 0f;
        bool isInLight = currentDanger > 0.1f; // Nếu danger > 0.1 nghĩa là đang bị đèn soi

        float facingDir = sr.flipX ? -1f : 1f;
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 attackCenter = (Vector2)transform.position + actualOffset;

        // Chỉ đánh nếu: Có mục tiêu + Trong tầm + KHÔNG bị đèn soi
        if (target != null && Vector2.Distance(attackCenter, target.position) <= attackRange && !isInLight)
        {
            TryAttack();
            rb.linearVelocity = Vector2.zero;
            anim.SetBool("isRunning", false);
        }
        else
        {
            // Nếu bị đèn soi, ExecuteContextSteering sẽ tự né dựa trên dangerWeight
            ExecuteContextSteering(target);
        }
    }

    Transform DecideTarget()
    {
        if (playerA == null && playerB == null) return null;
        PlayerHP hpA = playerA?.GetComponent<PlayerHP>();
        PlayerHP hpB = playerB?.GetComponent<PlayerHP>();

        float distA = (playerA && hpA != null && !hpA.IsDead) ? Vector2.Distance(transform.position, playerA.position) : float.MaxValue;
        float distB = (playerB && hpB != null && !hpB.IsDead) ? Vector2.Distance(transform.position, playerB.position) : float.MaxValue;

        if (!isAggroed && (distA <= detectionRadius || distB <= detectionRadius)) isAggroed = true;
        if (!isAggroed || (distA == float.MaxValue && distB == float.MaxValue)) { isAggroed = false; return null; }

        float scoreB = (playerB && !hpB.IsDead) ? (distanceWeight / Mathf.Max(distB, 0.1f)) + (isPlayerBRepairing ? 50f : 0f) : 0f;
        float scoreA = (playerA && !hpA.IsDead) ? (distanceWeight / Mathf.Max(distA, 0.1f)) : 0f;
        return scoreB > scoreA ? playerB : playerA;
    }

    void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            anim.SetTrigger("isAttacking");
            lastAttackTime = Time.time;
        }
    }

    public void ExecuteHit() => PerformDamageToPlayer();

    void PerformDamageToPlayer()
    {
        float facingDir = sr.flipX ? -1f : 1f;
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 attackCenter = (Vector2)transform.position + actualOffset;

        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(attackCenter, attackRange, playerLayer);

        foreach (Collider2D col in hitObjects)
        {
            PlayerHP playerHP = col.GetComponentInParent<PlayerHP>();
            if (playerHP != null && !playerHP.IsDead)
            {
                playerHP.TakeDamage(1);
                Debug.Log("<color=red>Chém trúng Player:</color> " + col.name);
                break;
            }
        }
    }

    void ExecuteContextSteering(Transform target)
    {
        Vector2 currentPos = transform.position;
        Vector2 bestDir = Vector2.zero;
        float currentSpeed = (target == null) ? patrolSpeed : moveSpeed;
        Vector2 currentVelocityDir = rb.linearVelocity.normalized;

        if (target == null)
        {
            bestDir = (patrolTarget - currentPos).normalized;
            if (Vector2.Distance(currentPos, patrolTarget) < 0.2f || (stuckTimer += Time.deltaTime) > 3f)
            { PickNewPatrolPoint(); stuckTimer = 0f; }
        }
        else
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
            anim.SetBool("isRunning", true);
        }
        else { rb.linearVelocity = Vector2.zero; anim.SetBool("isRunning", false); }
    }

    void PickNewPatrolPoint() { patrolTarget = startPos + Random.insideUnitCircle * patrolRadius; }
    public void TakeDamage() { if (isDead) return; health--; if (healthBar) healthBar.UpdateBar(health, maxHealth); if (health <= 0) StartCoroutine(DieRoutine()); else anim.SetTrigger("isHurt"); }
    IEnumerator DieRoutine() { isDead = true; rb.linearVelocity = Vector2.zero; anim.SetTrigger("isDead"); GetComponent<Collider2D>().enabled = false; this.enabled = false; yield return new WaitForSeconds(2f); Destroy(gameObject); }

    private void OnDrawGizmos()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        float facingDir = sr.flipX ? -1f : 1f;
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + actualOffset, attackRange);
    }
}