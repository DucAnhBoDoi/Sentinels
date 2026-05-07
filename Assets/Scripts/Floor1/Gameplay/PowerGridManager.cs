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

    // --- THÊM 1: BIẾN MẠNG ĐỂ KIỂM TRA ĐÃ NHẬN QUEST CHƯA ---
    private NetworkVariable<bool> isQuestAccepted = new NetworkVariable<bool>(false);

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 4. THAY HÀM START BẰNG ON NETWORK SPAWN
    public override void OnNetworkSpawn()
    {
        // Khi Server cộng điểm hoặc Nhận quest, Client sẽ tự động cập nhật UI
        fixedNodes.OnValueChanged += (prev, curr) => UpdateUI();
        totalNodes.OnValueChanged += (prev, curr) => UpdateUI();
        
        // --- THÊM 2: Lắng nghe sự kiện khi Quest được nhận ---
        isQuestAccepted.OnValueChanged += (prev, curr) => UpdateUI(); 

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

    // --- THÊM 3: HÀM NÀY SẼ ĐƯỢC GỌI KHI BẤM NÚT ACCEPT ---
    public void OnQuestAccepted()
    {
        // Bất kỳ ai bấm nút cũng sẽ gửi lệnh lên Server để đồng bộ cho cả phòng
        AcceptQuestServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AcceptQuestServerRpc()
    {
        // Bật công tắc -> Kích hoạt UpdateUI() trên mọi máy
        isQuestAccepted.Value = true; 
    }

    public void AddFixedNode()
    {
        if (!IsServer) return; // Bảo mật: Chỉ Server được duyệt điểm

        fixedNodes.Value++;

        if (fixedNodes.Value >= totalNodes.Value && totalNodes.Value > 0)
        {
            // --- GỌI HIỆU ỨNG UI ĐỒNG BỘ MẠNG TRƯỚC ---
            if (QuestUIManager.Instance != null)
            {
                QuestUIManager.Instance.TriggerQuestCompleteNetwork();
            }

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
            // --- THÊM 4: NẾU CHƯA NHẬN QUEST -> LUÔN LUÔN ẨN UI ĐI ---
            if (!isQuestAccepted.Value)
            {
                progressText.gameObject.SetActive(false);
                return; // Dừng hàm tại đây, không chạy xuống dưới nữa
            }

            // NẾU ĐÃ NHẬN QUEST RỒI THÌ CHẠY LOGIC NHƯ BÌNH THƯỜNG
            if (totalNodes.Value > 0 && fixedNodes.Value >= totalNodes.Value)
            {
                // Nếu đã sửa xong hết, ẩn dòng text này đi
                progressText.gameObject.SetActive(false);
            }
            else
            {
                // Nếu chưa sửa xong, đảm bảo nó đang bật và cập nhật con số
                progressText.gameObject.SetActive(true);
                progressText.text = $"Circuits: {fixedNodes.Value}/{totalNodes.Value}";
            }
        }
    }
}