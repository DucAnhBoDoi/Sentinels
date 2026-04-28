// ============================================================
// FILE: Assets/Scripts/Floor3/Network/TopicNetworkSync.cs
// ============================================================

using Unity.Netcode;
using Scripts.Floor3.UI;

namespace Scripts.Floor3.Network
{
    public class TopicNetworkSync : NetworkBehaviour
    {
        public static TopicNetworkSync Instance;
        
        // Cờ đánh dấu để không gửi lệnh mở bảng liên tục nhiều lần
        private bool _hasTriggeredPanel = false;

        private void Awake() 
        { 
            Instance = this; 
        }

        // ĐÂY CHÍNH LÀ CÁI HÀM MÀ UNITY ĐANG BÁO THIẾU ĐÂY NÀY:
        [ClientRpc]
        public void ShowTopicPanelClientRpc()
        {
            if (_hasTriggeredPanel) return;
            _hasTriggeredPanel = true;

            if (Floor3UIManager.Instance != null)
            {
                Floor3UIManager.Instance.ShowTopicSelection();
            }
        }

        [ClientRpc]
        public void SyncLoadingStateClientRpc()
        {
            if (TopicSelectionUI.Instance != null) TopicSelectionUI.Instance.ApplyLoadingState();
        }

        [ClientRpc]
        public void SyncReadyStateClientRpc(bool isFallback)
        {
            if (TopicSelectionUI.Instance != null) TopicSelectionUI.Instance.ApplyReadyState(isFallback);
        }

        [ClientRpc]
        public void SyncStartMissionClientRpc()
        {
            if (TopicSelectionUI.Instance != null) TopicSelectionUI.Instance.ApplyStartMission();
        }
    }
}