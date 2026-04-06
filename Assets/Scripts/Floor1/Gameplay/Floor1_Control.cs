// ══════════════════════════════════════════════════════
// FILE: Floor1_Control.cs (Đã fix lỗi chớp đèn bằng LateUpdate)
// ══════════════════════════════════════════════════════
using UnityEngine;
using UnityEngine.InputSystem;

public class Floor1_Control : MonoBehaviour
{
    public enum PlayerType { PlayerA, PlayerB }
    public PlayerType playerType;

    [Header("Cài đặt Player A (Đèn pin)")]
    public Transform flashlightTransform;

    [Header("Cài đặt Player B (Sửa điện)")]
    public float interactRange = 1.5f;
    public Vector2 actionOffset;

    void Start()
    {
        // Tự động tắt đèn lúc mới vào game (Chỉ áp dụng cho Player A)
        if (playerType == PlayerType.PlayerA && flashlightTransform != null)
        {
            flashlightTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!QuestPopupManager.isGameStarted) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // ----------------------------------------------------
        // LOGIC PLAYER A (Chỉ bắt phím bật/tắt)
        // ----------------------------------------------------
        if (playerType == PlayerType.PlayerA)
        {
            if (keyboard.fKey.wasPressedThisFrame && flashlightTransform != null)
            {
                bool isLightOn = flashlightTransform.gameObject.activeSelf;
                flashlightTransform.gameObject.SetActive(!isLightOn);
            }
        }
        
        // ----------------------------------------------------
        // LOGIC PLAYER B (Bắt phím E để sửa điện)
        // ----------------------------------------------------
        else if (playerType == PlayerType.PlayerB)
        {
            if (keyboard.eKey.wasPressedThisFrame) PerformRepair();

            bool repairing = keyboard.eKey.isPressed;
            foreach (var robot in UtilityRobotAI.allRobots)
                robot.isPlayerBRepairing = repairing;
        }
    }

    // ----------------------------------------------------
    // LATE UPDATE: Đảm bảo đèn pin xoay mượt mà, KHÔNG bị chớp
    // ----------------------------------------------------
    void LateUpdate()
    {
        if (playerType == PlayerType.PlayerA && flashlightTransform != null && flashlightTransform.gameObject.activeSelf)
        {
            if (Mouse.current == null || Camera.main == null) return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Camera.main.nearClipPlane));
            mouseWorldPosition.z = 0f; 

            Vector3 lookDirection = mouseWorldPosition - flashlightTransform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            
            flashlightTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
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