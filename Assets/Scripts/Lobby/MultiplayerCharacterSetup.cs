// MultiplayerCharacterSetup.cs
using Unity.Netcode;
using Unity.Cinemachine;
using UnityEngine;

public class MultiplayerCharacterSetup : NetworkBehaviour
{
    [Header("Cài đặt Vai trò")]
    [Tooltip("Tích vào nếu đây là Player A. Bỏ tích nếu là Player B.")]
    public bool isPlayerA = true;

    private CinemachineCamera _cam;

    // THÊM HÀM NÀY
    private bool AssignToHostByLobbyState()
    {
        return LobbySwapButton.hostPlaysPlayerA ? isPlayerA : !isPlayerA;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // ĐỔI DÒNG NÀY
            bool assignToHost = AssignToHostByLobbyState();

            if (assignToHost)
            {
                if (NetworkObject.OwnerClientId != NetworkManager.ServerClientId)
                    NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            else
            {
                NetworkManager.OnClientConnectedCallback += AssignToClient;
                if (NetworkManager.ConnectedClientsIds.Count > 1)
                {
                    ulong clientTargetId = NetworkManager.ConnectedClientsIds[1];
                    if (NetworkObject.OwnerClientId != clientTargetId)
                        NetworkObject.ChangeOwnership(clientTargetId);
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) NetworkManager.OnClientConnectedCallback -= AssignToClient;
    }

    private void AssignToClient(ulong clientId)
    {
        // SỬA TOÀN BỘ HÀM NÀY
        if (!IsServer || clientId == NetworkManager.ServerClientId) return;

        // dùng cùng logic với OnNetworkSpawn, không hard-code player B nữa
        if (!AssignToHostByLobbyState())
        {
            if (NetworkObject.OwnerClientId != clientId)
                NetworkObject.ChangeOwnership(clientId);
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (_cam == null)
            {
                _cam = FindFirstObjectByType<CinemachineCamera>();
            }
            if (_cam != null && _cam.Target.TrackingTarget != this.transform)
            {
                _cam.Target.TrackingTarget = this.transform;
            }
        }
    }
}
