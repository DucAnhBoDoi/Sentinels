using UnityEngine;
using System.Collections;

public class PlayerHP : MonoBehaviour
{
    [Header("Chỉ số sinh tồn")]
    public int maxHealth = 10;
    public int currentHealth;
    public HealthBar healthBar; // MỚI: Kéo HealthBar_Canvas vào đây

    [Header("Thời gian bất tử (Giây)")]
    public float invulnerabilityDuration = 1.0f;
    private bool isInvulnerable = false;

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
        // CẬP NHẬT THANH MÁU LÚC ĐẦU
        if (healthBar) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damageAmount;
        
        // CẬP NHẬT THANH MÁU KHI TRÚNG ĐÒN
        if (healthBar) healthBar.UpdateBar(currentHealth, maxHealth);

        Debug.Log(gameObject.name + " trúng đòn! HP còn: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("<color=red>GAME OVER:</color> " + gameObject.name + " đã gục ngã!");

        if (anim) anim.SetTrigger("isDead");
        if (movementScript) movementScript.enabled = false;

        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        if (GameOverManager.Instance != null)
        {
            Invoke("TriggerGameOverUI", 1.5f);
        }
    }

    void TriggerGameOverUI()
    {
        GameOverManager.Instance.ShowGameOver();
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;
    }
}