using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ShardCollector : NetworkBehaviour
{
    public float floatAmplitude = 0.3f;
    public float floatFrequency = 2f;
    public float pickupRange = 2f;
    public LayerMask playerLayer;

    private Vector3 startPos;
    private bool isCollected = false;

    void Start() { startPos = transform.position; }

    void Update()
    {
        // Hiệu ứng lơ lửng chạy nội bộ trên mọi máy cho mượt
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        if (!IsSpawned || isCollected) return;

        if (Keyboard.current.fKey.wasPressedThisFrame) CheckPickup();
    }

    void CheckPickup()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayer);
        foreach (Collider2D col in colliders)
        {
            NetworkObject playerNetObj = col.GetComponentInParent<NetworkObject>();
            
            // CHỈ GỬI LỆNH LÊN SERVER NẾU NHÂN VẬT ĐÓ LÀ CỦA MÌNH (Tránh việc nhặt dùm người khác)
            if (playerNetObj != null && playerNetObj.IsOwner)
            {
                PlayerHP ph = col.GetComponentInParent<PlayerHP>();
                if (ph != null && !ph.IsDead)
                {
                    ClaimShardServerRpc();
                    break;
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    void ClaimShardServerRpc()
    {
        if (isCollected) return;
        isCollected = true;

        if (Floor2Manager.Instance != null)
        {
            Floor2Manager.Instance.LevelComplete();
        }

        NetworkObject.Despawn(true); // Xóa mảnh vỡ trên toàn mạng
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}