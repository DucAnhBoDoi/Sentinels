using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Animator))]
public class SkeletonAI : NetworkBehaviour, IDamagable
{
    [Header("Tham chiếu")]
    public Transform playerA;
    public Transform playerB;
    public HealthBar healthBar;
    public LayerMask playerLayer;

    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Unity.Netcode.Components.NetworkAnimator netAnim;

    [Header("Chỉ số AI")]
    public float moveSpeed = 2.5f;
    public float patrolSpeed = 1.2f;
    public float detectionRadius = 8f;
    public float attackRange = 1.2f;
    public Vector2 actionOffset;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Trạng thái")]
    public int maxHealth = 3;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        3,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private bool isDead = false;
    private bool isAggroed = false;
    public bool isPlayerBRepairing = false;

    // --- THÊM CẤU HÌNH PARTICLE VÀ KNOCKBACK TẠI ĐÂY ---
    [Header("Hiệu ứng & Knockback")]
    public ParticleSystem hitParticles; // Hạt xịt máu / tia lửa
    public float knockbackForce = 5f;   // Lực đẩy văng ra
    public float knockbackDuration = 0.2f; // Thời gian bị choáng/văng
    private bool isKnockedBack = false;    // Cờ hiệu đánh dấu đang bị văng

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

    private NetworkVariable<bool> isFlipped = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        netAnim = GetComponent<Unity.Netcode.Components.NetworkAnimator>();
        allRobots.Add(this);
        startPos = transform.position;
        PickNewPatrolPoint();

        if (playerA == null) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (playerB == null) playerB = GameObject.Find("Player_B_Mechanic")?.transform;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
        isFlipped.OnValueChanged += OnFlipChanged;

        if (healthBar) healthBar.UpdateBar(currentHealth.Value, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isFlipped.OnValueChanged -= OnFlipChanged;
        allRobots.Remove(this);
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        if (healthBar) healthBar.UpdateBar(newValue, maxHealth);

        if (newValue <= 0 && !isDead)
        {
            StartCoroutine(DieRoutine());
        }
        else if (newValue < previousValue)
        {
            if (netAnim) netAnim.SetTrigger("isHurt");
            else anim.SetTrigger("isHurt");

            StartCoroutine(FlashRedRoutine());

            // --- CHẠY HIỆU ỨNG HẠT (PARTICLE) KHI BỊ CHÉM TRÚNG ---
            if (hitParticles != null)
            {
                hitParticles.Play();
            }
        }
    }

    private IEnumerator FlashRedRoutine()
    {
        if (sr == null) yield break;
        
        Color originalColor = Color.white; 
        sr.color = Color.red;              
        
        yield return new WaitForSeconds(0.15f); 
        
        sr.color = originalColor;          
    }

    private void OnFlipChanged(bool previous, bool current)
    {
        if (sr) sr.flipX = current;
    }

    void Update()
    {
        if (isDead) return;

        if (!IsServer) return;

        // --- NẾU ĐANG BỊ KNOCKBACK THÌ TẠM DỪNG ĐUỔI THEO PLAYER ---
        if (isKnockedBack) return; 

        Transform target = DecideTarget();

        float currentDanger = (InfluenceMap.Instance != null) ? InfluenceMap.Instance.GetDangerValue(transform.position) : 0f;
        bool isInLight = currentDanger > 0.1f;

        float facingDir = sr.flipX ? -1f : 1f;
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 attackCenter = (Vector2)transform.position + actualOffset;

        if (target != null && Vector2.Distance(attackCenter, target.position) <= attackRange && !isInLight)
        {
            TryAttack();
            rb.linearVelocity = Vector2.zero;

            if (netAnim) netAnim.Animator.SetBool("isRunning", false);
            else anim.SetBool("isRunning", false);
        }
        else
        {
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
            if (netAnim) netAnim.SetTrigger("isAttacking");
            else anim.SetTrigger("isAttacking");
            lastAttackTime = Time.time;
        }
    }

    public void ExecuteHit()
    {
        if (!IsServer) return;
        PerformDamageToPlayer();
    }

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

            isFlipped.Value = bestDir.x < 0;

            if (netAnim) netAnim.Animator.SetBool("isRunning", true);
            else anim.SetBool("isRunning", true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (netAnim) netAnim.Animator.SetBool("isRunning", false);
            else anim.SetBool("isRunning", false);
        }
    }

    void PickNewPatrolPoint() { patrolTarget = startPos + Random.insideUnitCircle * patrolRadius; }

    public void TakeDamage()
    {
        if (isDead) return;
        TakeDamageServerRpc(1);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageServerRpc(int damage)
    {
        if (isDead) return;
        currentHealth.Value -= damage;

        // --- GỌI XỬ LÝ KNOCKBACK TẠI ĐÂY (Server xử lý vật lý) ---
        StartCoroutine(KnockbackRoutine());
    }

    // --- COROUTINE ĐẨY LÙI QUÁI ---
    private IEnumerator KnockbackRoutine()
    {
        isKnockedBack = true; // Bật cờ hiệu để hàm Update không can thiệp vào di chuyển nữa

        // Tính toán hướng đẩy lùi (Đẩy ra xa khỏi Player gần nhất)
        Transform target = DecideTarget();
        Vector2 knockbackDir = Vector2.zero;

        if (target != null)
        {
            knockbackDir = (transform.position - target.position).normalized;
        }
        else
        {
            // Nếu xui xẻo không tìm thấy Player, đẩy ngược về phía sau lưng nó
            float facingDir = sr.flipX ? 1f : -1f; 
            knockbackDir = new Vector2(facingDir, 0);
        }

        // Tác dụng lực đẩy
        rb.linearVelocity = knockbackDir * knockbackForce;

        // Chờ 0.2 giây (cho nó văng đủ xa)
        yield return new WaitForSeconds(knockbackDuration);

        // Hồi phục lại trạng thái bình thường để nó dí Player tiếp
        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (netAnim) netAnim.SetTrigger("isDead");
        else anim.SetTrigger("isDead");

        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;

        yield return new WaitForSeconds(2f);

        gameObject.SetActive(false);

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            if (NetworkObject.IsSceneObject == true)
            {
                NetworkObject.Despawn(false);
            }
            else
            {             
                NetworkObject.Despawn(true);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        float facingDir = sr.flipX ? -1f : 1f;
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector2)transform.position + actualOffset, attackRange);
    }
}