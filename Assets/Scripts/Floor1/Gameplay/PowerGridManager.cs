// ══════════════════════════════════════════════════════
// FILE: PowerGridManager.cs (Đã đồng bộ mạng)
// ══════════════════════════════════════════════════════
using UnityEngine;
using TMPro; 
using Unity.Netcode; // 1. THÊM THƯ VIỆN MẠNG

// 2. KẾ THỪA NETWORK BEHAVIOUR
public class PowerGridManager : NetworkBehaviour
{
    public static PowerGridManager Instance;

    [Header("UI Hiển thị")]
    public TextMeshProUGUI progressText; 

    // 3. ĐỔI BIẾN THƯỜNG THÀNH BIẾN MẠNG (Chỉ Server được sửa, Client chỉ được đọc)
    private NetworkVariable<int> totalNodes = new NetworkVariable<int>(0);
    private NetworkVariable<int> fixedNodes = new NetworkVariable<int>(0);

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
    }

    // 4. THAY HÀM START BẰNG ON NETWORK SPAWN
    public override void OnNetworkSpawn()
    {
        // Khi Server cộng điểm, Client sẽ tự động cập nhật UI
        fixedNodes.OnValueChanged += (prev, curr) => UpdateUI();
        totalNodes.OnValueChanged += (prev, curr) => UpdateUI();
        
        UpdateUI();

        // CHỈ SERVER MỚI ĐƯỢC QUYỀN ĐẾM MẠCH ĐIỆN
        if (IsServer) 
        {
            // Delay một chút để chắc chắn GraphGenerator đã vẽ map xong
            Invoke(nameof(CalculateNodes), 1.5f);
        }
    }

    void CalculateNodes()
    {
        CircuitNode[] allNodes = Object.FindObjectsByType<CircuitNode>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var node in allNodes)
        {
            if (!node.isWire) 
            {
                count++;
            }
        }
        
        totalNodes.Value = count; // Gán vào biến mạng, Client sẽ tự biết là 20
        Debug.Log($"[PowerGrid] Đã đếm được {totalNodes.Value} Hộp nối cần sửa!");
    }

    public void AddFixedNode()
    {
        if (!IsServer) return; // Bảo mật: Chỉ Server được duyệt điểm

        fixedNodes.Value++;

        if (fixedNodes.Value >= totalNodes.Value && totalNodes.Value > 0)
        {
            if (Floor1Manager.Instance != null)
            {
                // Gọi hàm qua màn bên Floor1Manager
                Floor1Manager.Instance.LevelCompleteServerRpc();
            }
        }
    }

    void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Circuits: {fixedNodes.Value}/{totalNodes.Value}";
        }
    }
}