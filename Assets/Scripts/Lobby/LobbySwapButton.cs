using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class LobbySwapButton : NetworkBehaviour
{
    // Trí nhớ toàn cục: Giữ trạng thái Host chọn con nào để mang vào Tầng 1
    public static bool hostPlaysPlayerA = true; 

    [Header("Kéo thả 2 cái Wrapper UI vào đây:")]
    public Transform player1Wrapper;
    public Transform player2Wrapper;

    private Button _btn;

    void Start()
    {
        hostPlaysPlayerA = true;
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(OnSwapClicked);
    }

    void OnSwapClicked()
    {
        // CHỈ Host mới được quyền bấm nút đổi chỗ
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // Bắn lệnh gửi cho TẤT CẢ mọi người trong phòng (Kể cả Host và Client)
            SwapUIRpc();
        }
    }

    // Lệnh này sẽ được thực thi trên màn hình của TẤT CẢ người chơi
    [Rpc(SendTo.Everyone)]
    void SwapUIRpc()
    {
        // 1. Đảo ngược trí nhớ
        hostPlaysPlayerA = !hostPlaysPlayerA;

        // 2. HOÁN ĐỔI VỊ TRÍ 2 KHUNG UI TRÊN MÀN HÌNH
        // Lấy vị trí thứ tự hiện tại của 2 khung
        int index1 = player1Wrapper.GetSiblingIndex();
        int index2 = player2Wrapper.GetSiblingIndex();

        // Đổi chéo thứ tự cho nhau (Nếu bạn dùng Layout Group, chúng sẽ tự trượt qua lại)
        player1Wrapper.SetSiblingIndex(index2);
        player2Wrapper.SetSiblingIndex(index1);

        Debug.Log($"<color=yellow>[Lobby] Đổi ghế! Host hiện đang ngồi ở khung {(hostPlaysPlayerA ? "Bên Trái (Player A)" : "Bên Phải (Player B)")}</color>");
    }
}