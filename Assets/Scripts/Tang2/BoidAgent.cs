using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    public Transform target; // Sẽ kéo TheCore vào đây
    public float speed = 2.5f;
    public float separationDistance = 0.8f; // Khoảng cách để né đồng đội
    public float separationWeight = 2.0f;

    void Update()
    {
        if (target == null) return;

        Vector2 currentPos = transform.position;
        Vector2 force = Vector2.zero;

        // 1. Lực hướng về Lõi (The Core)
        Vector2 toTarget = ((Vector2)target.position - currentPos).normalized;
        force += toTarget;

        // 2. Lực né đồng đội (Separation) - Rất quan trọng để di chuyển mượt
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(currentPos, separationDistance);
        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject && neighbor.CompareTag("Enemy"))
            {
                Vector2 diff = currentPos - (Vector2)neighbor.transform.position;
                force += diff.normalized * separationWeight;
            }
        }

        transform.Translate(force.normalized * speed * Time.deltaTime);
    }
}