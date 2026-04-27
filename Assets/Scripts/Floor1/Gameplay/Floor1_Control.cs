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
        isFlashlightOn.OnValueChanged += OnFlashlightStateChanged;

        if (flashlightTransform != null && playerType == PlayerType.PlayerA)
        {
            flashlightTransform.gameObject.SetActive(isFlashlightOn.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        isFlashlightOn.OnValueChanged -= OnFlashlightStateChanged;
    }

    private void OnFlashlightStateChanged(bool previousState, bool newState)
    {
        if (flashlightTransform != null && playerType == PlayerType.PlayerA)
        {
            flashlightTransform.gameObject.SetActive(newState);
        }
    }

    void Update()
    {
        // --- CHỐT CHẶN BẢO VỆ MẠNG ---
        if (!IsSpawned || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) 
        {
            return;
        }

        if (!IsOwner) return;

        if (!QuestPopupManager.isGameStarted) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (playerType == PlayerType.PlayerA)
        {
            if (keyboard.fKey.wasPressedThisFrame)
            {
                isFlashlightOn.Value = !isFlashlightOn.Value;
            }
        }
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
        // --- CHỐT CHẶN BẢO VỆ MẠNG LÚC OUT GAME ---
        if (!IsSpawned || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) 
        {
            return;
        }

        if (playerType != PlayerType.PlayerA || flashlightTransform == null) return;

        if (IsOwner)
        {
            if (Mouse.current == null || Camera.main == null) return;

            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Camera.main.nearClipPlane));
            mouseWorldPosition.z = 0f;

            Vector3 lookDirection = mouseWorldPosition - flashlightTransform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            flashlightTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            
            flashlightAngle.Value = angle;
        }
        else
        {
            Quaternion targetRotation = Quaternion.Euler(0, 0, flashlightAngle.Value - 90f);
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
            if (node && !node.isWire)
            {
                // SỬA LỖI Ở ĐÂY: Dùng InfluenceMap để kiểm tra xem đèn pin có ĐANG chiếu trúng không
                bool isIlluminated = false;
                if (InfluenceMap.Instance != null)
                {
                    // Lấy cường độ sáng từ đèn pin soi vào vị trí của trạm điện
                    // Hàm GetDangerValue() sẽ trả về > 0 nếu nằm trong vùng sáng
                    float lightIntensity = InfluenceMap.Instance.GetDangerValue(node.transform.position);
                    isIlluminated = lightIntensity > 0f;
                }

                // Điều kiện mới: Mạch điện đang hiện VÀ đèn pin của Người A đang soi trực tiếp vào nó
                if (node.GetComponent<SpriteRenderer>().color.a > 0 && isIlluminated)
                {
                    node.FixNode();
                    Debug.Log("Player B: Đã sửa xong Hộp Nối!");
                    break;
                }
                else if (!isIlluminated)
                {
                    Debug.LogWarning("Player B: Không thể sửa! Người A chưa soi đèn trực tiếp vào hộp nối này!");
                }
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