using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerObject : NetworkBehaviour
{
    [HideInInspector] public NetworkVariable<FixedString64Bytes> NetUsername =
        new(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            return;
        }

        NetUsername.Value = PlayerPrefs.HasKey(nameof(PlayerPrefsKeys.S_UserName))
            ? new FixedString64Bytes(PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserName)))
            : new FixedString64Bytes("Default Player");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.LocalClient.PlayerObject = null;
    }
}