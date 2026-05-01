using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using TMPro;

public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("Giao diện")]
    public TextMeshProUGUI nameText;
    
    [HideInInspector]
    public NetworkVariable<FixedString32Bytes> NetPlayerName = new(
        "", 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        NetPlayerName.OnValueChanged += OnNameChanged;
        
        if (nameText != null)
        {
            nameText.text = NetPlayerName.Value.ToString();
        }

        // Chạy thử nếu Host đã có sẵn quyền Owner
        UpdateNameIfOwner();
    }

    public override void OnNetworkDespawn()
    {
        NetPlayerName.OnValueChanged -= OnNameChanged;
    }

    public override void OnGainedOwnership()
    {
        UpdateNameIfOwner();
    }

    private void UpdateNameIfOwner()
    {
        if (IsOwner)
        {
            string playerName = "Default Player";
            if (PlayerPrefs.HasKey(nameof(PlayerPrefsKeys.S_UserName)))
            {
                playerName = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName));
            }
            NetPlayerName.Value = playerName; 
        }
    }

    private void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (nameText != null)
        {
            nameText.text = newValue.ToString();
        }
    }
}