using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [Header("Chỉ số sinh tồn")]
    public int maxHealth = 10;
    public int currentHealth;
    public HealthBar healthBar; 

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

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        if (healthBar) healthBar.UpdateBar(currentHealth, maxHealth);

        Debug.Log($"<color=red>{gameObject.name} trúng đòn!</color> HP còn: {currentHealth}");

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (anim) anim.SetTrigger("isDead");
        if (movementScript) movementScript.enabled = false;

        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        if (GameOverManager.Instance != null)
        {
            Invoke("TriggerGameOverUI", 1.5f);
        }
    }

    void TriggerGameOverUI() => GameOverManager.Instance.ShowGameOver();
}