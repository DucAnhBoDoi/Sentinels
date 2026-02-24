using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour 
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider hpSlider; 
    public Image fillImage;   // Kéo Fill Image vào đây

    void Start()
    {
        currentHealth = maxHealth;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
            hpSlider.value = currentHealth;
        }

        if (fillImage != null)
        {
            fillImage.color = Color.green; // Màu mặc định
        }
    }

    void Update()
    {
        if (hpSlider != null)
        {
            hpSlider.value = currentHealth;
        }

        // Đổi màu theo lượng máu
        if (fillImage != null)
        {
            float healthPercent = currentHealth / maxHealth;

            if (healthPercent > 0.5f)
                fillImage.color = Color.green;
            else if (healthPercent > 0.2f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " đã kiệt sức vì cứu Lõi!");
        }
    }
}
