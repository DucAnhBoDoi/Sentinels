using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image fillImage; 
    
    [Header("Cài đặt UI")]
    [Tooltip("Tích vào nếu thanh máu gắn trên nhân vật di chuyển. Bỏ tích nếu dán cố định trên màn hình (Canvas).")]
    public bool isWorldSpaceUI = true; // CÔNG TẮC PHÂN BIỆT

    private Quaternion initialRotation;

    void Start()
    {
        // Chỉ lưu góc xoay nếu nó gắn trên nhân vật
        if (isWorldSpaceUI) 
        {
            initialRotation = transform.rotation;
        }
    }

    void LateUpdate()
    {
        // Chỉ thực hiện logic chống lật nếu nó gắn trên nhân vật
        if (isWorldSpaceUI && transform.parent != null)
        {
            transform.rotation = initialRotation;

            Vector3 parentScale = transform.parent.localScale;
            transform.localScale = new Vector3(
                Mathf.Sign(parentScale.x) * Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    public void UpdateBar(float current, float max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = current / max;
        }
    }
}