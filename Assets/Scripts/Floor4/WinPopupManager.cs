using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
// Đã xóa bớt thư viện UI và SceneManagement không cần thiết

public class WinPopupManager : NetworkBehaviour
{
    // Bỏ _popup và _btnBackToMenu vì không dùng UI cũ nữa
    [SerializeField] private BossPhase1 _bossPhase2;
    [SerializeField] private float _bossPhase2DeathDuration;

    private void Start()
    {
        if (_bossPhase2 != null)
        {
            _bossPhase2.OnDeath += OnBossPhase2Death;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_bossPhase2 != null)
        {
            _bossPhase2.OnDeath -= OnBossPhase2Death; // Sửa lại cách gỡ event cho chuẩn
        }
    }

    private void OnBossPhase2Death()
    {
        // Chỉ Server mới được quyền đếm giờ và ra lệnh chuyển cảnh End Game
        if (!IsServer) return;

        // Vẫn dùng DOTween chờ Boss diễn xong hoạt ảnh chết y như code cũ
        DOTween.Sequence()
            .AppendInterval(_bossPhase2DeathDuration)
            .OnComplete(() => 
            {
                // SAU KHI BOSS NGÃ XUỐNG XONG -> GỌI ĐẠO DIỄN TẦNG 4 BẤM MÁY CHIẾU PHIM!
                if (Floor4Manager.Instance != null)
                {
                    Floor4Manager.Instance.LevelCompleteServerRpc();
                }
            });
    }
}