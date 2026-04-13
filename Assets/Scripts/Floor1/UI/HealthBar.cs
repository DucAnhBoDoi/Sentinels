using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image fillImage; // Kéo ảnh Fill (màu đỏ/xanh) vào đây
    private Quaternion initialRotation;

    void Start()
    {
        // Giữ hướng xoay ban đầu để thanh máu không bị lật khi nhân vật quay mặt
        initialRotation = transform.rotation;
    }

    // Cập nhật lại script HealthBar.cs
    void LateUpdate()
    {
        
        transform.rotation = initialRotation;

        Vector3 parentScale = transform.parent.localScale;
        transform.localScale = new Vector3(
            Mathf.Sign(parentScale.x) * Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );
    }

    public void UpdateBar(float current, float max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = current / max;
        }
    }
}