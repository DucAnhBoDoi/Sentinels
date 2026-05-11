using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinPopupManager : NetworkBehaviour
{
    [SerializeField] private RectTransform _popup;
    [SerializeField] private BossPhase1 _bossPhase2;
    [SerializeField] private float _bossPhase2DeathDuration;
    [SerializeField] private Button _btnBackToMenu;

    private void Start()
    {
        _bossPhase2.OnDeath += OnBossPhase2Death;
        _btnBackToMenu.onClick.AddListener(OnBtnBackToMenuClicked);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _bossPhase2.OnDeath = null;
        _btnBackToMenu.onClick.RemoveAllListeners();
    }

    private void OnBtnBackToMenuClicked()
    {
        NetworkManager.Shutdown();
        Time.timeScale = 1;
    }

    private void OnBossPhase2Death()
    {
        DOTween.Sequence()
            .AppendInterval(_bossPhase2DeathDuration)
            .OnComplete(ShowPopupClientRpc);
    }

    [ClientRpc]
    private void ShowPopupClientRpc()
    {
        _popup.gameObject.SetActive(true);
        Time.timeScale = 0;
    }
}