using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Cấu hình máu")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Giao diện UI")]
    public Slider healthSlider; 

    // Biến này được cập nhật từ script bệ sạc (ChargePlatform)
    public bool isOnPlatform = false; 

    void Start()
    {
        // Khởi tạo máu ban đầu
        currentHealth = maxHealth;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Update()
    {
        // Cập nhật thanh máu liên tục theo giá trị hiện tại
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(float damage) {
        // Trừ máu
        currentHealth -= damage;
        
        // Kiểm tra nếu máu về 0
        if (currentHealth <= 0) {
            currentHealth = 0;
            Debug.Log(gameObject.name + " đã hết máu! Gọi Game Over.");
            
            // Tìm script LifeCore bất kể nó nằm ở đối tượng nào trong Scene
            LifeCore core = Object.FindAnyObjectByType<LifeCore>();
            if (core != null) {
                // Gọi hàm kết thúc game công khai trong LifeCore
                core.GameOver(); 
            }
        }
    }

    public void Heal(float amount) {
        // Hồi máu và đảm bảo không vượt quá giới hạn
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }
}