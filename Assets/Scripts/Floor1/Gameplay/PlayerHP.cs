using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [Header("Chỉ số sinh tồn")]
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar healthBar; 

    [Header("Cơ chế Tầng 2")]
    public bool isOnPlatform = false; 

    private bool isDead = false; 
    public bool IsDead => isDead;

    private Animator anim;
    private Rigidbody2D rb;
    private PlayerMovement movementScript;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<PlayerMovement>();

        currentHealth = maxHealth;
        if (healthBar) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthBar) healthBar.UpdateBar(currentHealth, maxHealth);

        if (damageAmount > 0)
        {
            Debug.Log($"<color=red>{gameObject.name} trúng đòn!</color> HP còn: {currentHealth}");
        }

        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        TakeDamage(-amount);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // 1. Chạy Animation chết (Ngã xuống)
        if (anim) anim.SetTrigger("isDead");

        // 2. Khóa di chuyển
        if (movementScript) movementScript.enabled = false;

        // 3. Dừng vật lý và va chạm
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        // 4. CHỜ 1.5 GIÂY RỒI MỚI HIỆN BẢNG (Dùng chung cho mọi tầng)
        if (GameOverManager.Instance != null)
        {
            Invoke("TriggerGameOverUI", 1.5f);
        }
    }

    // Hàm này sẽ được gọi sau 1.5 giây
    void TriggerGameOverUI()
    {
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
    }
}