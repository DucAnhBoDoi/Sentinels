using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class UtilityRobotAI_Floor3 : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerA;
    public Transform playerB;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float patrolSpeed = 1.5f;
    public float detectionRadius = 7f;

    [Header("Patrol")]
    public float patrolRadius = 4f;
    public float stuckTimeLimit = 3f;

    [Header("Steering")]
    public int scanDirections = 12;
    public float scanStepSize = 1.5f;
    public float robotRadius = 0.4f;

    [Header("Obstacle")]
    public LayerMask obstacleLayer;

    public static List<UtilityRobotAI_Floor3> allRobots = new List<UtilityRobotAI_Floor3>();

    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private Vector2 startPos;
    private Vector2 patrolTarget;

    private float stuckTimer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        allRobots.Add(this);

        startPos = transform.position;
        PickNewPatrolPoint();

        if (playerA == null)
        {
            GameObject pa = GameObject.Find("Player_A_Navigator");
            if (pa) playerA = pa.transform;
        }

        if (playerB == null)
        {
            GameObject pb = GameObject.Find("Player_B_Mechanic");
            if (pb) playerB = pb.transform;
        }
    }

    void OnDestroy()
    {
        allRobots.Remove(this);
    }

    void Update()
    {
        Transform target = FindNearestPlayer();
        ExecuteMovement(target);
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

            stuckTimer += Time.deltaTime;

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

            Vector2 idealDir = ((Vector2)target.position - currentPos).normalized;
            float highestScore = float.MinValue;

            for (int i = 0; i < scanDirections; i++)
            {
                float angle = (360f / scanDirections) * i * Mathf.Deg2Rad;

                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                float score = Vector2.Dot(dir, idealDir);

                RaycastHit2D hit = Physics2D.CircleCast(
                    currentPos,
                    robotRadius,
                    dir,
                    scanStepSize,
                    obstacleLayer
                );

                if (hit.collider != null)
                    score -= 10f;

                if (score > highestScore)
                {
                    highestScore = score;
                    bestDir = dir;
                }
            }
        }

        if (bestDir != Vector2.zero)
        {
            rb.linearVelocity = bestDir.normalized * currentSpeed;

            if (Mathf.Abs(bestDir.x) > 0.1f)
                sr.flipX = bestDir.x < 0;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
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
    }
}