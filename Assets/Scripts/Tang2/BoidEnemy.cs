using UnityEngine;
using System.Collections.Generic;

public class BoidEnemy : MonoBehaviour
{
    [Header("Thông số di chuyển & Máu")]
    public float speed = 0.5f;
    public float currentHealth = 30f;
    public float retreatHealthThreshold = 10f; 
    public float attackRange = 1.2f;
    public float damageToCore = 15f;

    [Header("Thông số Bầy đàn (Boids)")]
    public float neighborRadius = 3.0f;    
    public float separationWeight = 2.5f; 
    public float alignmentWeight = 1.0f;  
    public float cohesionWeight = 1.0f;   
    public float penaltyAvoidanceWeight = 3.0f; 

    [Header("Thông số Né Nhân Vật")]
    public float playerAvoidanceRadius = 4.0f; // Khoảng cách bắt đầu né người chơi
    public float playerAvoidanceWeight = 5.0f; // Lực đẩy né người chơi (để cao để quái quyết tâm né)

    private Transform target; 
    private LifeCore coreScript;
    private bool isRetreating = false;
    private static List<BoidEnemy> allBoids = new List<BoidEnemy>();
    private static List<Vector2> penaltyPoints = new List<Vector2>();

    void OnEnable() { allBoids.Add(this); }
    void OnDisable() { allBoids.Remove(this); }

    void Start()
    {
        GameObject core = GameObject.FindGameObjectWithTag("TheCore");
        if (core != null)
        {
            target = core.transform;
            coreScript = core.GetComponent<LifeCore>();
        }
    }

    void Update()
    {
        if (target == null) return;

        ExecuteBehaviorTree();

        Vector2 boidMove = CalculateBoidVelocity();
        Vector2 penaltyAvoidance = CalculatePenaltyAvoidance();
        
        // THÊM: Tính toán lực né nhân vật
        Vector2 playerAvoidance = CalculatePlayerAvoidance();
        
        Vector2 seekDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        if (isRetreating) seekDir *= -1.5f; 

        // Kết hợp tất cả các lực: Hướng về Lõi + Bầy đàn + Né vùng chết + Né người chơi
        Vector2 finalDirection = (boidMove + seekDir + penaltyAvoidance + playerAvoidance).normalized;
        transform.position += (Vector3)finalDirection * speed * Time.deltaTime;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance <= attackRange && !isRetreating)
        {
            AttackAndDie();
        }
    }

    void ExecuteBehaviorTree()
    {
        if (currentHealth <= retreatHealthThreshold)
        {
            isRetreating = true;
            return;
        }
    }

    // THUẬT TOÁN NÉ NHÂN VẬT
    Vector2 CalculatePlayerAvoidance()
    {
        Vector2 avoidance = Vector2.zero;
        // Tìm tất cả các đối tượng có Tag là Player
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject player in players)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist < playerAvoidanceRadius)
            {
                // Tạo lực đẩy tỉ lệ nghịch với khoảng cách (càng gần đẩy càng mạnh)
                Vector2 pushDir = (Vector2)transform.position - (Vector2)player.transform.position;
                avoidance += pushDir.normalized / (dist + 0.1f);
            }
        }
        return avoidance * playerAvoidanceWeight;
    }

    Vector2 CalculateBoidVelocity()
    {
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        int neighborsCount = 0;

        foreach (BoidEnemy boid in allBoids)
        {
            if (boid == this) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);

            if (dist < neighborRadius)
            {
                separation += (Vector2)(transform.position - boid.transform.position).normalized / (dist + 0.1f);
                alignment += (Vector2)boid.transform.right;
                cohesion += (Vector2)boid.transform.position;
                neighborsCount++;
            }
        }

        if (neighborsCount > 0)
        {
            separation = (separation / neighborsCount) * separationWeight;
            alignment = (alignment / neighborsCount) * alignmentWeight;
            cohesion = ((cohesion / neighborsCount - (Vector2)transform.position).normalized) * cohesionWeight;
            return (separation + alignment + cohesion).normalized;
        }
        return Vector2.zero;
    }

    Vector2 CalculatePenaltyAvoidance()
    {
        Vector2 avoidance = Vector2.zero;
        foreach (Vector2 deathPoint in penaltyPoints)
        {
            float dist = Vector2.Distance(transform.position, deathPoint);
            if (dist < 4.0f) 
            {
                avoidance += (Vector2)transform.position - deathPoint;
            }
        }
        return avoidance.normalized * penaltyAvoidanceWeight;
    }

    void AttackAndDie()
    {
        if (coreScript != null)
        {
            coreScript.isUnderAttack = true;
            coreScript.TakeDirectDamage(damageToCore);
        }
        RecordDeathPosition();
        Destroy(gameObject);
    }

    void RecordDeathPosition()
    {
        penaltyPoints.Add(transform.position);
        if (penaltyPoints.Count > 10) penaltyPoints.RemoveAt(0);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
    }
}