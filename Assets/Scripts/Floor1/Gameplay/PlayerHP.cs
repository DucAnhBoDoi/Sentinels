using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerHP : NetworkBehaviour
{
    [Header("Chỉ số sinh tồn")] public float maxHealth = 100f;

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public HealthBar healthBar;

    [Header("Cơ chế Tầng 2")] public bool isOnPlatform = false;

    private bool isDead = false;
    public bool IsDead => isDead;

    private Animator anim;
    private Rigidbody2D rb;
    private PlayerMovement movementScript;
    private Unity.Netcode.Components.NetworkAnimator netAnim;

    private SpriteRenderer sr; // BIẾN CHỨA HÌNH ẢNH NHÂN VẬT

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<PlayerMovement>();
        netAnim = GetComponent<Unity.Netcode.Components.NetworkAnimator>();

        // Lấy SpriteRenderer để tí nữa đổi màu
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;

        if (healthBar) healthBar.UpdateBar(currentHealth.Value, maxHealth);
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        // Cập nhật UI thanh máu
        if (healthBar) healthBar.UpdateBar(newValue, maxHealth);

        // Nổ log nếu bị trừ máu
        if (newValue < previousValue && newValue > 0)
        {
            Debug.Log($"<color=red>{gameObject.name} trúng đòn!</color> HP còn: {newValue}");

            // --- GỌI HIỆU ỨNG CHỚP ĐỎ TẠI ĐÂY ---
            StartCoroutine(FlashRedRoutine());
        }

        // Nếu máu <= 0 và chưa chết -> Gọi hàm Die() trên CẢ 2 MÁY
        if (newValue <= 0 && !isDead)
        {
            Die();
        }
    }

    // --- COROUTINE CHỚP ĐỎ CỦA PLAYER ---
    private IEnumerator FlashRedRoutine()
    {
        if (sr == null) yield break;

        Color originalColor = Color.white; // Màu gốc

        // Đổi toàn bộ màu nhân vật thành Đỏ
        sr.color = Color.red;

        // Khựng lại 0.15 giây (bằng đúng thời gian chớp của con quái)
        yield return new WaitForSeconds(0.15f);

        // Trả màu về bình thường
        sr.color = originalColor;
    }

    // Bất kỳ ai (Client hay Quái) gọi hàm này đều sẽ gửi thư lên Server
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        if (NetworkManager && NetworkManager.IsClient)
        {
            TakeDamageServerRpc(damageAmount);
            return;
        }

        float newHealth = currentHealth.Value - damageAmount;
        currentHealth.Value = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    // 6. GỬI SERVER (ĐÃ CẬP NHẬT THEO CHUẨN UNITY MỚI NHẤT)
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageServerRpc(float damageAmount)
    {
        if (isDead) return;

        float newHealth = currentHealth.Value - damageAmount;
        currentHealth.Value = Mathf.Clamp(newHealth, 0, maxHealth);
    }

    public void Heal(float amount)
    {
        TakeDamage(-amount);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. Chạy Animation chết qua mạng
        if (netAnim) netAnim.SetTrigger("isDead");
        else if (anim) anim.SetTrigger("isDead");

        // 2. Khóa di chuyển
        if (movementScript) movementScript.enabled = false;

        // 3. Dừng vật lý và va chạm
        rb.linearVelocity = Vector2.zero;
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // 4. CHỜ 1.5 GIÂY RỒI MỚI HIỆN BẢNG
        if (GameOverManager.Instance != null)
        {
            Invoke("TriggerGameOverUI", 1.5f);
        }
    }

    void TriggerGameOverUI()
    {
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }
}