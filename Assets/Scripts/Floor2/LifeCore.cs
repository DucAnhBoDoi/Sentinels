using UnityEngine;
using UnityEngine.UI;

public class LifeCore : MonoBehaviour
{
    [Header("Thông số máu")]
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float chargeSpeed = 20f; 

    [Header("Giao diện UI")]
    public Slider energyBar;
    public GameObject gameOverPanel;

    [Header("Trạng thái")]
    public bool isUnderAttack = false;
    public int playersOnPlatforms = 0; 

    void Start() {
        if (energyBar != null) {
            energyBar.minValue = 0;
            energyBar.maxValue = maxEnergy;
            energyBar.value = energy;
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update() {
        if (energyBar != null) energyBar.value = energy;

        // LOGIC HỒI MÁU LÕI & TRỪ MÁU TẤT CẢ PLAYER ĐANG SẠC
        if (playersOnPlatforms > 0 && energy < maxEnergy) {
            energy += chargeSpeed * Time.deltaTime;

            // Tìm tất cả đối tượng có script PlayerHealth trong Scene
            PlayerHealth[] allPlayers = Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            
            foreach (PlayerHealth ph in allPlayers) {
                // CHỈ TRỪ MÁU những ai đang thực sự đứng trên bệ sạc
                if (ph.isOnPlatform) {
                    ph.TakeDamage(10f * Time.deltaTime); 
                }
            }
        }

        if (energy >= maxEnergy) {
            energy = maxEnergy;
            isUnderAttack = false;
        }

        if (energy <= 0) {
            energy = 0;
            GameOver();
        }
    }

    // Hàm PUBLIC để PlayerHealth có thể gọi khi Player hết máu
    public void GameOver() {
        if (gameOverPanel != null) {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0; 
        }
    }

    public void SetPlayerOnPlatform(bool isOnPlatform) {
        if (isOnPlatform) playersOnPlatforms++;
        else playersOnPlatforms--;
        playersOnPlatforms = Mathf.Max(0, playersOnPlatforms);
    }

    public void TakeDirectDamage(float amount) {
        energy -= amount;
        isUnderAttack = true; 
    }
}