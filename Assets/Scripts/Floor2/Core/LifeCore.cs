using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.Tilemaps; // THÊM ĐỂ XỬ LÝ TILEMAP
using System.Collections;   // THÊM ĐỂ DÙNG COROUTINE (IEnumerator)

public class LifeCore : NetworkBehaviour
{
    [Header("Thông số máu")]
    public float maxEnergy = 100f;
    public float chargeSpeed = 20f;

    public NetworkVariable<float> energy = new NetworkVariable<float>(0f);
    public NetworkVariable<int> playersOnPlatforms = new NetworkVariable<int>(0);

    [Header("Cơ chế Co-op")]
    public int requiredPlayersToCharge = 2;

    [Header("Giao diện UI")]
    public HealthBar energyBar;
    public TextMeshProUGUI hpText;

    [Header("Trạng thái")]
    public bool isUnderAttack = false;

    // --- HIỆU ỨNG LÕI BỊ ĐÁNH ---
    [Header("Hiệu ứng chớp trắng")]
    public TilemapRenderer coreTopRenderer;    // Dành cho lớp WalkBehind
    public TilemapRenderer coreBottomRenderer; // Dành cho lớp Collision
    public Material whiteFlashMaterial;
    
    private Material originalMaterialTop;
    private Material originalMaterialBottom;

    public override void OnNetworkSpawn()
    {
        if (IsServer) energy.Value = maxEnergy;

        energy.OnValueChanged += (prev, current) => UpdateUI(current);

        UpdateUI(energy.Value);

        // Lưu lại màu gốc của 2 lớp Tilemap khi vừa vào game
        if (coreTopRenderer != null) originalMaterialTop = coreTopRenderer.material;
        if (coreBottomRenderer != null) originalMaterialBottom = coreBottomRenderer.material;
    }

    void Update()
    {
        if (!IsServer) return;

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
        if (!IsServer) return; 
        if (isOnPlatform) playersOnPlatforms.Value++;
        else playersOnPlatforms.Value--;
        playersOnPlatforms.Value = Mathf.Max(0, playersOnPlatforms.Value);
    }

    public void TakeDirectDamage(float amount)
    {
        if (!IsServer) return;
        if (energy.Value <= 0) return; 

        energy.Value -= amount;
        if (energy.Value < 0) energy.Value = 0; 

        isUnderAttack = true;

        // Bắn tín hiệu chớp trắng cho mọi máy
        FlashCoreClientRpc();
    }
    
    // --- XỬ LÝ NHÁY TRẮNG MÀN HÌNH ---
    [ClientRpc]
    private void FlashCoreClientRpc()
    {
        if (whiteFlashMaterial != null)
        {
            StartCoroutine(FlashWhiteRoutine());
        }
    }

    private IEnumerator FlashWhiteRoutine()
    {
        // Đổi cả 2 phần của Lõi sang màu trắng
        if (coreTopRenderer != null) coreTopRenderer.material = whiteFlashMaterial;
        if (coreBottomRenderer != null) coreBottomRenderer.material = whiteFlashMaterial;
        
        yield return new WaitForSeconds(0.1f);
        
        // Trả lại màu gốc
        if (coreTopRenderer != null) coreTopRenderer.material = originalMaterialTop;
        if (coreBottomRenderer != null) coreBottomRenderer.material = originalMaterialBottom;
    }
}