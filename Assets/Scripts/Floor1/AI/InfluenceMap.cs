using UnityEngine;

public class InfluenceMap : MonoBehaviour
{
    public static InfluenceMap Instance;

    [Header("Nguồn sáng (Đèn pin)")]
    public Transform flashlight;
    public float lightRange = 10f, lightAngle = 60f;
    public float maxDangerValue = 2000f, dangerFalloff = 1.5f;

    void Awake() { if (!Instance) Instance = this; else Destroy(gameObject); }

    public float GetDangerValue(Vector2 worldPosition)
    {
        if (!flashlight || !flashlight.gameObject.activeInHierarchy) return 0f;

        Vector2 dirToCell = worldPosition - (Vector2)flashlight.position;
        float dist = dirToCell.magnitude;

        // Nếu nằm ngoài tầm xa HOẶC chệch khỏi góc chiếu -> An toàn tuyệt đối
        if (dist > lightRange || Vector2.Angle(flashlight.up, dirToCell) > lightAngle / 2f) 
            return 0f;

        // Trong vùng sáng -> Tính cường độ nguy hiểm
        return maxDangerValue * (1f - Mathf.Pow(dist / lightRange, dangerFalloff));
    }
}