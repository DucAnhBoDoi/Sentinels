using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG
using Scripts.Floor3.Gameplay;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
// ĐỔI SANG NetworkBehaviour
public class UtilityRobotAI_Floor3 : NetworkBehaviour, IDamagable
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

    // ── THÊM CẤU HÌNH HIỆU ỨNG ──
    [Header("Hiệu ứng")]
    public ParticleSystem hitParticles;

    public static List<UtilityRobotAI_Floor3> allRobots = new List<UtilityRobotAI_Floor3>();

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private HitReactionController _hitReaction;
    private Vector2 startPos;
    private Vector2 patrolTarget;

    private float stuckTimer = 0f;
    private float currentCooldown = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        allRobots.Add(this);

        startPos = transform.position;
        PickNewPatrolPoint();

        if (playerA == null) playerA = GameObject.Find("Player_A_Navigator")?.transform;
        if (playerB == null) playerB = GameObject.Find("Player_B_Mechanic")?.transform;

        _hitReaction = GetComponent<HitReactionController>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        allRobots.Remove(this);
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        if (!Scripts.Floor3.UI.TopicSelectionUI.hasStartedMission) return;
        if (_hitReaction != null && _hitReaction.IsBeingKnockedBack) return;

        if (currentCooldown > 0)
            currentCooldown -= Time.fixedDeltaTime;

        Transform target = FindNearestPlayer();

        if (target != null)
        {
            ExecuteMovement(target);
            TickAttack();
        }
        else
        {
            ExecuteMovement(null);
        }
    }

    void TickAttack()
    {
        if (currentCooldown > 0) return;

        float attackRadius = stoppingDistance + 0.3f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);

        foreach (Collider2D hit in hits)
        {
            PlayerHP hp = hit.GetComponent<PlayerHP>();
            if (hp != null && !hp.IsDead)
            {
                hp.TakeDamage(attackDamage);
                currentCooldown = attackCooldown;
                return;
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
                bestDir = Vector2.zero;
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
            rb.linearVelocity = bestDir.normalized * currentSpeed;
        else
            rb.linearVelocity = Vector2.zero;

        bool shouldFlip = sr.flipX;
        if (target != null)
        {
            if (target.position.x < transform.position.x - 0.1f) shouldFlip = true;
            else if (target.position.x > transform.position.x + 0.1f) shouldFlip = false;
        }
        else if (Mathf.Abs(bestDir.x) > 0.1f)
        {
            shouldFlip = bestDir.x < 0;
        }

        if (sr.flipX != shouldFlip)
        {
            sr.flipX = shouldFlip;
            SyncFlipClientRpc(shouldFlip);
        }
    }

    [ClientRpc]
    private void SyncFlipClientRpc(bool flip)
    {
        if (!IsServer && sr != null) sr.flipX = flip;
    }

    void PickNewPatrolPoint()
    {
        patrolTarget = startPos + Random.insideUnitCircle * patrolRadius;
        stuckTimer = 0f;
    }

    public void TakeDamage() { TakeDamage(1f); }

    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        if (_hitReaction == null) { DespawnVirus(); return; }

        Transform attacker = FindNearestPlayer();
        Vector2 knockbackDir = Vector2.up;
        if (attacker != null)
            knockbackDir = ((Vector2)transform.position - (Vector2)attacker.position).normalized;

        // Bật hiệu ứng trên Server (Host)
        if (hitParticles != null) hitParticles.Play();
        StartCoroutine(FlashRedRoutine());

        // Kêu Client bật hiệu ứng
        TriggerHitVisualClientRpc(knockbackDir);

        bool died = _hitReaction.ReactToHit(knockbackDir, amount);
        if (died) DespawnVirus();
    }

    [ClientRpc]
    private void TriggerHitVisualClientRpc(Vector2 dir)
    {
        if (IsServer)
        {
            // Server xử lý chính
            hitParticles?.Play();
            StartCoroutine(FlashRedRoutine());

            _hitReaction?.ReactOnly(dir);
            GetComponent<Virus2AnimatorController>()?.TriggerHurt();
        }
        else
        {
            // Client chỉ hiển thị hiệu ứng (không logic)
            _hitReaction?.ReactOnly(dir);
            GetComponent<Virus2AnimatorController>()?.TriggerHurt();
        }
    }

    // --- COROUTINE CHỚP ĐỎ ---
    private IEnumerator FlashRedRoutine()
    {
        if (sr == null) yield break;
        Color originalColor = Color.white;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        sr.color = originalColor;
    }

    private void DespawnVirus()
    {
        // THÊM DÒNG NÀY TRƯỚC KHI DESPAWN:
        GetComponent<Virus2AnimatorController>()?.TriggerDeath();

        // Delay nhỏ để Death animation kịp play
        StartCoroutine(DelayedDespawn());

        if (NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        else Destroy(gameObject);
    }

    private System.Collections.IEnumerator DelayedDespawn()
    {
        yield return new WaitForSeconds(0.5f); // khớp với độ dài clip Death
        if (NetworkObject.IsSpawned) NetworkObject.Despawn(true);
        else Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(Application.isPlaying ? startPos : (Vector2)transform.position, patrolRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, stoppingDistance + 0.3f);
    }
}