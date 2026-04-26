using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class Floor1_Control : NetworkBehaviour
{
    public enum PlayerType { PlayerA, PlayerB }
    public PlayerType playerType;

    [Header("Cài đặt Player A (Đèn pin)")]
    public Transform flashlightTransform;

    public NetworkVariable<bool> isFlashlightOn = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<float> flashlightAngle = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Cài đặt Player B (Sửa điện)")]
    public float interactRange = 1.5f;
    public Vector2 actionOffset;

    public override void OnNetworkSpawn()
    {
        // Lắng nghe sự kiện Bật/Tắt đèn
        isFlashlightOn.OnValueChanged += OnFlashlightStateChanged;

        // Khởi tạo trạng thái ban đầu của đèn
        if (flashlightTransform != null && playerType == PlayerType.PlayerA)
        {
            flashlightTransform.gameObject.SetActive(isFlashlightOn.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        isFlashlightOn.OnValueChanged -= OnFlashlightStateChanged;
    }

    // Hàm tự động chạy trên mọi máy khi biến công tắc thay đổi
    private void OnFlashlightStateChanged(bool previousState, bool newState)
    {
        if (flashlightTransform != null && playerType == PlayerType.PlayerA)
        {
            flashlightTransform.gameObject.SetActive(newState);
        }
    }

    void Update()
    {
        // KẾT HỢP: Ngăn lỗi NullReferenceException nếu có Player khác mà mình không điều khiển
        if (!IsOwner) return;

        if (!QuestPopupManager.isGameStarted) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ----------------------------------------------------
        // LOGIC PLAYER A (Chỉ bắt phím bật/tắt)
        // ----------------------------------------------------
        if (playerType == PlayerType.PlayerA)
        {
            if (keyboard.fKey.wasPressedThisFrame)
            {
                // Thay vì tự tắt bật, giờ mình gạt công tắc mạng
                isFlashlightOn.Value = !isFlashlightOn.Value;
            }
        }

        // ----------------------------------------------------
        // LOGIC PLAYER B (Bắt phím E để sửa điện)
        // ----------------------------------------------------
        else if (playerType == PlayerType.PlayerB)
        {
            if (keyboard.eKey.wasPressedThisFrame) PerformRepair();

            bool repairing = keyboard.eKey.isPressed;
            foreach (var skeleton in SkeletonAI.allRobots)
                skeleton.isPlayerBRepairing = repairing;
        }
    }

    void LateUpdate()
    {
        if (playerType != PlayerType.PlayerA || flashlightTransform == null) return;

        if (IsOwner)
        {
            if (Mouse.current == null || Camera.main == null) return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Camera.main.nearClipPlane));
            mouseWorldPosition.z = 0f;

            Vector3 lookDirection = mouseWorldPosition - flashlightTransform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // Xoay đèn trực tiếp cho chính mình xem thật mượt (60 FPS)
            flashlightTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // Báo góc xoay lên mạng (30 FPS)
            flashlightAngle.Value = angle;
        }
        // NẾU MÌNH LÀ NGƯỜI NHÌN (Thấy người khác chĩa đèn)
        else
        {
            // SỬA CHỖ NÀY: Dùng Lerp để làm mượt góc xoay
            Quaternion targetRotation = Quaternion.Euler(0, 0, flashlightAngle.Value - 90f);

            // Xoay trượt từ góc hiện tại tới góc mục tiêu với tốc độ 15f
            flashlightTransform.rotation = Quaternion.Lerp(flashlightTransform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    void PerformRepair()
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);
        Vector2 centerPoint = (Vector2)transform.position + actualOffset;

        foreach (Collider2D col in Physics2D.OverlapCircleAll(centerPoint, interactRange))
        {
            var node = col.GetComponent<CircuitNode>();
            if (node && !node.isWire && node.GetComponent<SpriteRenderer>().color.a > 0)
            {
                node.FixNode();
                Debug.Log("Player B: Đã sửa xong Hộp Nối!");
                break;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (playerType == PlayerType.PlayerB)
        {
            float facingDir = Mathf.Sign(transform.localScale.x);
            Vector2 actualOffset = new Vector2(actionOffset.x * facingDir, actionOffset.y);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere((Vector2)transform.position + actualOffset, interactRange);
        }
    }
}