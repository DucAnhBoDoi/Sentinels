using System;
using DG.Tweening;
using UnityEngine;

public class BossRoomEnterTrigger : MonoBehaviour
{
    [SerializeField] private GameObject _gate;

    // [SerializeField] private BossController _boss;

    // [SerializeField] private GameObject _bossAvatar;

    [SerializeField] private BossPhase1 _bossPhase1;
    [SerializeField] private BossPhase1 _bossPhase2;
    [SerializeField] private float _bossPhase1DeathDuration;

    private void Awake()
    {
        _bossPhase1.enabled = false;
        _bossPhase1.OnDeath += OnBossPhase1Death;
        _bossPhase2.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }

        _gate.SetActive(true);
        // _boss.gameObject.SetActive(true);
        // Destroy(_bossAvatar);
        _bossPhase1.enabled = true;
        Destroy(gameObject);
    }

    private void OnBossPhase1Death()
    {
        DOTween.Sequence()
            .AppendInterval(_bossPhase1DeathDuration)
            .OnComplete(() =>
            {
                Destroy(_bossPhase1.gameObject);
                _bossPhase2.gameObject.SetActive(true);
            });
    }
}