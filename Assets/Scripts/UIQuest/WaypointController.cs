using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))] 
public class WaypointController : MonoBehaviour
{
    [Header("Mục tiêu cần chỉ đường")]
    public Transform target; 

    [Header("Cấu hình Icon")]
    [Tooltip("Khoảng cách cách lề màn hình (pixel)")]
    public float edgePadding = 50f; 

    // --- THÊM DÒNG NÀY: Biến để giữ cái chữ [ENTER] ---
    [Header("Chữ Hướng Dẫn (Tùy chọn)")]
    public GameObject enterTextObj; 

    private RectTransform rectTransform;
    private Camera mainCamera;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
        
        if (target == null)
        {
            GameObject door = GameObject.Find("Elevator_Trigger");
            if (door != null) target = door.transform;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        rectTransform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (target == null || !gameObject.activeInHierarchy) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

        if (screenPos.z < 0) screenPos *= -1;

        float screenHalfWidth = Screen.width / 2;
        float screenHalfHeight = Screen.height / 2;
        Vector2 posOnScreenRelativeCenter = new Vector2(screenPos.x - screenHalfWidth, screenPos.y - screenHalfHeight);

        float clampX = screenHalfWidth - edgePadding;
        float clampY = screenHalfHeight - edgePadding;

        // KIỂM TRA XEM CỬA CÓ ĐANG Ở NGOÀI MÀN HÌNH KHÔNG
        bool isOffScreen = Mathf.Abs(posOnScreenRelativeCenter.x) > clampX || Mathf.Abs(posOnScreenRelativeCenter.y) > clampY;

        if (isOffScreen)
        {
            float exceedRatioX = Mathf.Abs(posOnScreenRelativeCenter.x) / clampX;
            float exceedRatioY = Mathf.Abs(posOnScreenRelativeCenter.y) / clampY;

            if (exceedRatioX > exceedRatioY)
            {
                posOnScreenRelativeCenter.x = Mathf.Sign(posOnScreenRelativeCenter.x) * clampX;
                posOnScreenRelativeCenter.y /= exceedRatioX; 
            }
            else
            {
                posOnScreenRelativeCenter.y = Mathf.Sign(posOnScreenRelativeCenter.y) * clampY;
                posOnScreenRelativeCenter.x /= exceedRatioY; 
            }

            // --- TẮT CHỮ [ENTER] ĐI VÌ ĐANG Ở NGOÀI RÌA MÀN HÌNH ---
            if (enterTextObj != null && enterTextObj.activeSelf)
            {
                enterTextObj.SetActive(false);
            }
        }
        else
        {
            posOnScreenRelativeCenter.y += 60f; 

            // --- BẬT CHỮ [ENTER] LÊN VÌ ĐÃ NHÌN THẤY CÁI CỬA ---
            if (enterTextObj != null && !enterTextObj.activeSelf)
            {
                enterTextObj.SetActive(true);
            }
        }

        rectTransform.anchoredPosition = posOnScreenRelativeCenter;
    }
}