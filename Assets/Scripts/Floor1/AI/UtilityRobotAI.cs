using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))] 
public class UtilityRobotAI : MonoBehaviour
{
    [Header("Tham chiếu mục tiêu")]
    public Transform playerA;
    public Transform playerB;

    [Header("Chỉ số & Trọng số AI")]
    public float moveSpeed = 3f, patrolSpeed = 1.5f, detectionRadius = 8f;
    public bool isPlayerBRepairing = false;
    public float patrolRadius = 4f, stuckTimeLimit = 3f;
    
    public int scanDirections = 16; 
    public float scanStepSize = 1.5f; 
    public float robotRadius = 0.4f; 
    
    // Cập nhật trọng số: Giảm bớt sức nặng của khoảng cách để AI ưu tiên việc "Lách vòng ngoài" hơn
    public float distanceWeight = 5f, dangerWeight = 2f, separationWeight = 50f, neighborRadius = 3f;
    
    [Tooltip("Trọng số Quán tính: Giúp quái trượt mượt qua góc tường/đèn mà không bị kẹt")]
    public float momentumWeight = 8f; 

    [Header("Tương tác Vật lý")]
    public LayerMask obstacleLayer; 

    public static List<UtilityRobotAI> allRobots = new List<UtilityRobotAI>();
    private SpriteRenderer sr;
    private Rigidbody2D rb; 
    private Vector2 startPos, patrolTarget;
    private float stuckTimer = 0f;

    private bool isAggroed = false;

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
            if (pa != null) playerA = pa.transform;
        }
        if (playerB == null) 
        {
            GameObject pb = GameObject.Find("Player_B_Mechanic");
            if (pb != null) playerB = pb.transform;
        }
    }
    
    void OnDestroy() => allRobots.Remove(this);

    void Update() => ExecuteContextSteering(DecideTarget());

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
        float currentSpeed = patrolSpeed;

        // Lấy hướng đang di chuyển hiện tại để tạo Quán tính
        Vector2 currentVelocityDir = rb.linearVelocity.normalized;

        if (target == null) // ĐI TUẦN
        {
            bestDir = (patrolTarget - currentPos).normalized;
            stuckTimer += Time.deltaTime;
            
            if (Vector2.Distance(currentPos, patrolTarget) < 0.2f || stuckTimer > stuckTimeLimit || 
                Physics2D.CircleCast(currentPos, robotRadius, bestDir, 0.5f, obstacleLayer))
            {
                PickNewPatrolPoint();
            }
        }
        else // TRUY SÁT & LÁCH TƯỜNG / ĐÈN PIN (FLANKING)
        {
            currentSpeed = moveSpeed;
            float highestScore = float.MinValue;
            
            Vector2 idealDir = ((Vector2)target.position - currentPos).normalized;

            for (int i = 0; i < scanDirections; i++)
            {
                float rad = (360f / scanDirections * i) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 samplePos = currentPos + dir * scanStepSize;

                // 1. KHAO KHÁT: Muốn lao tới người chơi
                float score = Vector2.Dot(dir, idealDir) * distanceWeight;

                // 2. QUÁN TÍNH (THUẬT TOÁN MỚI CHỐNG KẸT): Ưu tiên giữ nguyên hướng trượt để lách qua mép
                if (currentVelocityDir != Vector2.zero)
                {
                    score += Vector2.Dot(dir, currentVelocityDir) * momentumWeight;
                }

                // 3. NÉ ĐÈN PIN: Nhận diện Ma Trận Nhiệt
                if (InfluenceMap.Instance) 
                {
                    float danger = InfluenceMap.Instance.GetDangerValue(samplePos);
                    score -= danger * dangerWeight; 
                }

                // 4. NÉ TƯỜNG CHUẨN XÁC
                RaycastHit2D hit = Physics2D.CircleCast(currentPos, robotRadius, dir, scanStepSize, obstacleLayer);
                if (hit.collider != null)
                {
                    score -= 10000f;
                }

                // 5. TRÁNH ĐỒNG ĐỘI
                foreach (var bot in allRobots)
                {
                    if (bot != this)
                    {
                        float dist = Vector2.Distance(samplePos, bot.transform.position);
                        if (dist < neighborRadius) score -= separationWeight / Mathf.Max(dist, 0.1f);
                    }
                }

                if (score > highestScore) 
                { 
                    highestScore = score; 
                    bestDir = dir; 
                }
            }
        }

        // --- DI CHUYỂN BẰNG VẬT LÝ ---
        if (bestDir != Vector2.zero)
        {
            rb.linearVelocity = bestDir.normalized * currentSpeed; 
            if (Mathf.Abs(bestDir.x) > 0.1f) sr.flipX = bestDir.x < 0; 
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void PickNewPatrolPoint() { patrolTarget = startPos + Random.insideUnitCircle * patrolRadius; stuckTimer = 0f; }

    public void TakeDamage()
    {
        Debug.Log("Robot đã bị Người B đánh nát!");
        Destroy(gameObject); 
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(Application.isPlaying ? startPos : (Vector2)transform.position, patrolRadius);
    }
}