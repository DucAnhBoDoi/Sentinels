using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LifeCore : MonoBehaviour
{
    [Header("Thông số máu")]
    public float energy = 100f;
    public float maxEnergy = 100f;
    public float chargeSpeed = 20f;

    [Header("Cơ chế Co-op")]
    [Tooltip("Số lượng người chơi CẦN THIẾT đứng trên bệ để bắt đầu sạc lõi")]
    public int requiredPlayersToCharge = 2; // CHỈNH SỐ 2 Ở ĐÂY

    [Header("Giao diện UI")]
    public HealthBar energyBar;
    public TextMeshProUGUI hpText;
    public GameObject gameOverPanel;

    [Header("Trạng thái")]
    public bool isUnderAttack = false;
    public int playersOnPlatforms = 0;

    void Start()
    {
        if (energyBar != null)
        {
            energyBar.UpdateBar(energy, maxEnergy);
        }
        UpdateHpDisplay();
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (energyBar != null) energyBar.UpdateBar(energy, maxEnergy);
        UpdateHpDisplay();

        // LOGIC SẠC LÕI MỚI: Chỉ sạc khi ĐỦ số người yêu cầu (playersOnPlatforms >= 2)
        if (playersOnPlatforms >= requiredPlayersToCharge && energy < maxEnergy)
        {
            energy += chargeSpeed * Time.deltaTime;

            // Rút máu của NHỮNG NGƯỜI đang đứng trên bệ
            PlayerHP[] allPlayers = Object.FindObjectsByType<PlayerHP>(FindObjectsSortMode.None);

            foreach (PlayerHP ph in allPlayers)
            {
                if (ph.isOnPlatform)
                {
                    ph.TakeDamage(10f * Time.deltaTime);
                }
            }
        }

        if (energy >= maxEnergy)
        {
            energy = maxEnergy;
            isUnderAttack = false;
        }

        if (energy <= 0)
        {
            energy = 0;
            GameOver();
        }
    }

    void UpdateHpDisplay()
    {
        if (hpText != null) hpText.text = Mathf.RoundToInt(energy) + " / " + maxEnergy;
    }

    public void GameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0;
        }
    }

    public void SetPlayerOnPlatform(bool isOnPlatform)
    {
        if (isOnPlatform) playersOnPlatforms++;
        else playersOnPlatforms--;
        playersOnPlatforms = Mathf.Max(0, playersOnPlatforms);
    }

    public void TakeDirectDamage(float amount)
    {
        energy -= amount;
        isUnderAttack = true;
    }
}