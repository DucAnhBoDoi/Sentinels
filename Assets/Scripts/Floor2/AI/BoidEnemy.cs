using UnityEngine;
using System.Collections.Generic;

public class BoidEnemy : MonoBehaviour
{
    [Header("Boid Settings")]
    public float speed = 3.0f;
    public float neighborRadius = 3.5f; 
    [Range(0, 5)] public float separationWeight = 2.5f; 
    [Range(0, 5)] public float cohesionWeight = 1.5f;   
    [Range(0, 5)] public float alignmentWeight = 1.2f;  
    [Range(0, 5)] public float targetWeight = 1.2f; // Tăng nhẹ lực hút Lõi

    [Header("Cấu hình tấn công")]
    public float attackDistance = 1.0f; // Khoảng cách để quái nổ sát thương

    private Transform target;
    private Vector2 currentVelocity;
    private static List<BoidEnemy> allBoids = new List<BoidEnemy>();
    public static Dictionary<Vector2Int, float> penaltyMap = new Dictionary<Vector2Int, float>();

    void OnEnable() { allBoids.Add(this); }
    void OnDisable() { allBoids.Remove(this); }

    void Start() {
        GameObject core = GameObject.FindGameObjectWithTag("TheCore");
        if (core != null) target = core.transform;
        currentVelocity = Random.insideUnitCircle.normalized * speed;
    }

    void Update() {
        if (target == null) return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // NẾU ĐÃ GẦN LÕI: Bỏ qua bầy đàn, lao thẳng vào để tấn công
        if (distanceToTarget < 2.0f) {
            Vector2 attackDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            transform.Translate(attackDir * speed * Time.deltaTime, Space.World);
            
            if (distanceToTarget < attackDistance) {
                LifeCore coreScript = target.GetComponent<LifeCore>();
                if(coreScript != null) coreScript.TakeDirectDamage(10f);
                Destroy(gameObject);
            }
            return; // Thoát hàm để không chạy logic bầy đàn bên dưới
        }

        // LOGIC BẦY ĐÀN (Chỉ chạy khi ở xa Lõi)
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
        transform.Translate(currentVelocity * Time.deltaTime, Space.World);
    }

    private void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;
        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
        penaltyMap[gridPos] = Time.time + 5.0f;
    }
}