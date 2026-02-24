using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LifeCore : MonoBehaviour
{
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float chargeSpeed = 20f; // Tăng tốc độ nạp cho nhanh hơn
    public float drainDamage = 10f;

    public bool isUnderAttack = false;
    public Slider energyBar;
    public GameObject gameOverPanel;

    void Start()
    {
        energy = maxEnergy;
        Time.timeScale = 1f; 
        if (energyBar != null)
        {
            energyBar.maxValue = maxEnergy;
            energyBar.value = energy;
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        UpdateEnergyUI();

        // Nếu năng lượng nạp đầy, tắt trạng thái bị tấn công để ngừng nạp phí máu
        if (energy >= maxEnergy)
        {
            isUnderAttack = false;
        }

        if (energy <= 0)
        {
            GameOver();
        }
    }

    public void TakeDirectDamage(float amount)
    {
        energy -= amount;
        isUnderAttack = true; // Bật trạng thái nạp máu ngay khi bị quái đâm
        energy = Mathf.Clamp(energy, 0, maxEnergy);
    }

    void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ĐÂY LÀ PHẦN QUAN TRỌNG NHẤT ĐỂ HỒI MÁU
    void OnTriggerStay2D(Collider2D other)
    {
        // 1. Kiểm tra nếu là Player đứng trong vùng
        if (other.CompareTag("Player"))
        {
            // 2. Chỉ hồi máu khi Lõi đã bị mất máu (isUnderAttack) và năng lượng chưa đầy
            if (isUnderAttack && energy < maxEnergy)
            {
                energy += chargeSpeed * Time.deltaTime;
                
                // Trừ máu người chơi đang đứng nạp cho Lõi
                PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(drainDamage * Time.deltaTime);
                }
                
                Debug.Log("Đang nạp năng lượng cho Lõi...");
            }
        }
    }

    void UpdateEnergyUI()
    {
        if (energyBar != null)
        {
            energyBar.value = energy;
            Image fillImage = energyBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(Color.red, Color.green, energy / maxEnergy);
            }
        }
    }
}