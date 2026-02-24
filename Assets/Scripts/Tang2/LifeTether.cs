using UnityEngine;

public class LifeTether : MonoBehaviour 
{
    public Transform player1;
    public Transform player2;
    public float maxLinkDistance = 8f;

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        if (player1 == null || player2 == null) return;
        lineRenderer.SetPosition(0, player1.position);
        lineRenderer.SetPosition(1, player2.position);

        float dist = Vector2.Distance(player1.position, player2.position);
        lineRenderer.startColor = lineRenderer.endColor = (dist > maxLinkDistance) ? Color.red : Color.cyan;
    }

    // THÊM ĐOẠN NÀY VÀO: Đây là phần mà LifeCore đang tìm kiếm
    public bool IsLinkActive()
    {
        if (player1 == null || player2 == null) return false;
        return Vector2.Distance(player1.position, player2.position) <= maxLinkDistance;
    }
}