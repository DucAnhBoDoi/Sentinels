using UnityEngine;
using TMPro;
using Unity.Netcode;

public class LifeCore : NetworkBehaviour
{
    [Header("Thông số máu")]
    public float maxEnergy = 100f;
    public float chargeSpeed = 20f;

    // Khởi tạo 0f để tránh cảnh báo mạng, Server sẽ tự nạp maxEnergy
    public NetworkVariable<float> energy = new NetworkVariable<float>(0f);
    public NetworkVariable<int> playersOnPlatforms = new NetworkVariable<int>(0);

    [Header("Cơ chế Co-op")]
    public int requiredPlayersToCharge = 2;

    [Header("Giao diện UI")]
    public HealthBar energyBar;
    public TextMeshProUGUI hpText;

    [Header("Trạng thái")]
    public bool isUnderAttack = false;

    public override void OnNetworkSpawn()
    {
        // Gán bằng maxEnergy
        if (IsServer) energy.Value = maxEnergy;

        // Lắng nghe sự thay đổi máu để cập nhật UI
        energy.OnValueChanged += (prev, current) => UpdateUI(current);

        UpdateUI(energy.Value);
    }

    void Update()
    {
        // CHỈ SERVER MỚI ĐƯỢC TÍNH TOÁN SẠC/TRỪ MÁU
        if (!IsServer) return;

        // Thêm chốt chặn && energy.Value > 0 để lõi nổ rồi thì không trừ máu người chơi nữa
        if (playersOnPlatforms.Value >= requiredPlayersToCharge && energy.Value < maxEnergy && energy.Value > 0)
        {
            energy.Value += chargeSpeed * Time.deltaTime;

            PlayerHP[] allPlayers = Object.FindObjectsByType<PlayerHP>(FindObjectsSortMode.None);
            foreach (PlayerHP ph in allPlayers)
            {
                if (ph.isOnPlatform) ph.TakeDamage(10f * Time.deltaTime);
            }
        }

        if (energy.Value >= maxEnergy)
        {
            energy.Value = maxEnergy;
            isUnderAttack = false;
        }

        if (energy.Value <= 0)
        {
            energy.Value = 0;
            // Báo cho mọi người game over qua Floor2Manager (Dùng chung bảng Game Over)
            if (Floor2Manager.Instance != null)
            {
                Floor2Manager.Instance.TriggerGameOverServerRpc();
            }
        }
    }

    void UpdateUI(float currentEnergy)
    {
        if (energyBar != null) energyBar.UpdateBar(currentEnergy, maxEnergy);
        if (hpText != null) hpText.text = Mathf.RoundToInt(currentEnergy) + " / " + maxEnergy;
    }

    public void SetPlayerOnPlatform(bool isOnPlatform)
    {
        if (!IsServer) return; // Chỉ Server đếm số người
        if (isOnPlatform) playersOnPlatforms.Value++;
        else playersOnPlatforms.Value--;
        playersOnPlatforms.Value = Mathf.Max(0, playersOnPlatforms.Value);
    }

    public void TakeDirectDamage(float amount)
    {
        if (!IsServer) return;

        // SỬA Ở ĐÂY: Nếu lõi đã nổ (máu <= 0) thì chặn đứng mọi luồng sát thương tới
        if (energy.Value <= 0) return; 

        energy.Value -= amount;

        // SỬA Ở ĐÂY: Ép máu không bao giờ được tụt xuống dưới số 0
        if (energy.Value < 0) energy.Value = 0; 

        isUnderAttack = true;
    }
}