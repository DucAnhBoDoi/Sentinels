using UnityEngine;
using Unity.Netcode;

public class CircuitNode : NetworkBehaviour
{
    private SpriteRenderer spriteRenderer;
    
    public NetworkVariable<bool> isFixedNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isVisibleNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Đánh dấu loại Prefab")]
    public bool isWire = false;

    [Header("Gắn hình ảnh mạch điện vào đây")]
    public Sprite darkSprite;
    public Sprite glowingSprite;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) 
        {
            spriteRenderer.sprite = darkSprite;
            // Chỉnh cứng: Tắt hiển thị thay vì đổi màu mờ
            spriteRenderer.enabled = false; 
        }
    }

    public override void OnNetworkSpawn()
    {
        isFixedNet.OnValueChanged += OnNodeFixed;
        
        isVisibleNet.OnValueChanged += (oldVal, newVal) => {
            SetVisibility(newVal);
        };

        if (isFixedNet.Value)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = glowingSprite;
        }
        
        SetVisibility(isVisibleNet.Value);
    }

    public override void OnNetworkDespawn()
    {
        isFixedNet.OnValueChanged -= OnNodeFixed;
        isVisibleNet.OnValueChanged -= (oldVal, newVal) => SetVisibility(newVal);
    }

    private void OnNodeFixed(bool previous, bool current)
    {
        if (current && !previous) 
        {
            if (spriteRenderer != null) spriteRenderer.sprite = glowingSprite; 

            if (IsServer)
            {
                PropagateElectricity();
                if (!isWire && PowerGridManager.Instance != null)
                {
                    PowerGridManager.Instance.AddFixedNode();
                }
            }
        }
    }

    public void FixNode()
    {
        if (!isFixedNet.Value) FixNodeServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void FixNodeServerRpc()
    {
        if (!isFixedNet.Value) isFixedNet.Value = true; 
    }

    void PropagateElectricity()
    {
        Vector2[] positionsToCheck = {
            (Vector2)transform.position + Vector2.up,
            (Vector2)transform.position + Vector2.down,
            (Vector2)transform.position + Vector2.left,
            (Vector2)transform.position + Vector2.right
        };

        foreach (Vector2 pos in positionsToCheck)
        {
            Collider2D[] colliders = Physics2D.OverlapPointAll(pos);
            foreach (Collider2D col in colliders)
            {
                CircuitNode neighbor = col.GetComponent<CircuitNode>();
                if (neighbor != null && neighbor != this && !neighbor.isFixedNet.Value && neighbor.isWire)
                {
                    neighbor.isFixedNet.Value = true; 
                }
            }
        }
    }

    public void SetVisibility(bool isVisible)
    {
        if (spriteRenderer == null) return;
        
        // Bật tắt hẳn hình ảnh, đảm bảo Client 100% nhận lệnh
        spriteRenderer.enabled = isVisible;
        
        // Vẫn giữ lệnh chỉnh màu a=1 để an toàn nếu prefab lỡ lưu màu mờ
        Color color = spriteRenderer.color;
        color.a = 1f; 
        spriteRenderer.color = color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsServer && other.CompareTag("Flashlight")) isVisibleNet.Value = true;
    }

    // THÊM Hàm Stay: Đảm bảo nếu chuột vẩy nhanh quá Server lỡ nhịp thì nó gánh lại
    void OnTriggerStay2D(Collider2D other)
    {
        if (IsServer && !isVisibleNet.Value && other.CompareTag("Flashlight"))
            isVisibleNet.Value = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (IsServer && other.CompareTag("Flashlight")) isVisibleNet.Value = false;
    }
}