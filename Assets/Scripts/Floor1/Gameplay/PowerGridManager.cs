using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PowerGridManager : NetworkBehaviour
{
    public static PowerGridManager Instance;

    [Header("UI Hiển thị")]
    public TextMeshProUGUI progressText;

    private NetworkVariable<int> totalNodes = new NetworkVariable<int>(0);
    private NetworkVariable<int> fixedNodes = new NetworkVariable<int>(0);

    private NetworkVariable<bool> isQuestAccepted = new NetworkVariable<bool>(false);

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Khi Server cộng điểm hoặc Nhận quest, Client sẽ tự động cập nhật UI
        fixedNodes.OnValueChanged += (prev, curr) => UpdateUI();
        totalNodes.OnValueChanged += (prev, curr) => UpdateUI();
        
        isQuestAccepted.OnValueChanged += (prev, curr) => UpdateUI(); 

        if (IsServer)
        {
            if (QuestPopupManager.hasAcceptedOnce)
            {
                isQuestAccepted.Value = true;
            }

            // Delay một chút để chắc chắn GraphGenerator đã vẽ map xong
            Invoke(nameof(CalculateNodes), 1.5f);
        }

        UpdateUI();
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

    public void OnQuestAccepted()
    {
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
            // Chưa nhận quest --> ẩn UI ---
            if (!isQuestAccepted.Value)
            {
                progressText.gameObject.SetActive(false);
                return; 
            }
            if (totalNodes.Value > 0 && fixedNodes.Value >= totalNodes.Value)
            {
                progressText.gameObject.SetActive(false);
            }
            else
            {
                progressText.gameObject.SetActive(true);
                progressText.text = $"Circuits: {fixedNodes.Value}/{totalNodes.Value}";
            }
        }
    }
}