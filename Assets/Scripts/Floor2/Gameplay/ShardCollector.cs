using UnityEngine;
using UnityEngine.InputSystem; // Nếu bạn dùng Input System mới

public class ShardCollector : MonoBehaviour
{
    [Header("Cài đặt hiệu ứng")]
    public float floatAmplitude = 0.3f;
    public float floatFrequency = 2f;
    
    [Header("Cài đặt nhặt đồ")]
    public float pickupRange = 2f; // Khoảng cách có thể nhặt (nên để 2 hoặc 3)
    
    private Vector3 startPos;
    private GameObject player;

    void Start()
    {
        startPos = transform.position;
        // Tự động tìm Player trong cảnh
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        // 1. Hiệu ứng lơ lửng nhấp nhô
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 2. Kiểm tra nhấn phím F và khoảng cách
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            CheckPickup();
        }
    }

    void CheckPickup()
    {
        if (player == null) return;

        // Tính khoảng cách giữa Player và Shard
        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= pickupRange)
        {
            Debug.Log("<color=cyan>Victory!</color> Đã nhấn F để nhặt Shard!");
            
            // Thực hiện logic thắng cuộc
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                // gm.WinGame(); // Hoặc bất kỳ hàm nào bạn muốn gọi
            }

            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Bạn ở quá xa để nhặt! Khoảng cách hiện tại: " + distance);
        }
    }

    // Vẽ một vòng tròn nhỏ trong Scene để bạn dễ hình dung phạm vi nhặt
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}