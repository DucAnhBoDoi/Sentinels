using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinPopupManager : MonoBehaviour
{
    [SerializeField] private RectTransform _popup;
    [SerializeField] private BossPhase1 _bossPhase2;
    [SerializeField] private Button _btnBackToMenu;

    private void Start()
    {
        _bossPhase2.OnDeath += OnBossPhase2Death;
        _btnBackToMenu.onClick.AddListener(OnBtnBackToMenuClicked);
    }

    private void OnBtnBackToMenuClicked()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MenuScene");
    }

    private void OnBossPhase2Death()
    {
        _popup.gameObject.SetActive(true);
    }
}
