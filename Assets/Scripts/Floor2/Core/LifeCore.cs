using UnityEngine;
using UnityEngine.UI;
using TMPro; // BẮT BUỘC có dòng này để dùng TextMeshPro

public class LifeCore : MonoBehaviour
{
    [Header("Thông số máu")]
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float chargeSpeed = 20f; 

    [Header("Giao diện UI")]
    public Slider energyBar;
    public TextMeshProUGUI hpText; // Ô mới để kéo HpText vào
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
        
        UpdateHpDisplay(); // Cập nhật chữ lúc mới vào game

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update() {
        if (energyBar != null) energyBar.value = energy;
        
        UpdateHpDisplay(); // Luôn cập nhật chữ khi máu thay đổi

        // LOGIC HỒI MÁU LÕI & TRỪ MÁU TẤT CẢ PLAYER ĐANG SẠC
        if (playersOnPlatforms > 0 && energy < maxEnergy) {
            energy += chargeSpeed * Time.deltaTime;

            PlayerHealth[] allPlayers = Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            
            foreach (PlayerHealth ph in allPlayers) {
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

    // HÀM MỚI: Hiển thị con số lên màn hình
    void UpdateHpDisplay() {
        if (hpText != null) {
            // Hiển thị định dạng "100 / 100" giống ảnh bạn muốn
            hpText.text = Mathf.RoundToInt(energy) + " / " + maxEnergy;
        }
    }

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