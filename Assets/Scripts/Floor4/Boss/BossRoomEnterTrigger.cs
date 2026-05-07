using System;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class BossRoomEnterTrigger : NetworkBehaviour
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
        _bossPhase2.transform.parent.gameObject.SetActive(false);
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

        if (NetworkManager && NetworkManager.IsClient)
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (player.TryGetComponent(out NetworkObject nObj) && nObj.IsOwner)
                {
                    player.transform.position = transform.position;
                }
            }

            NetworkObject.Despawn();
        }
        else
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                player.transform.position = transform.position;
            }

            Destroy(gameObject);
        }
    }

    private void OnBossPhase1Death()
    {
        DOTween.Sequence()
            .AppendInterval(_bossPhase1DeathDuration)
            .OnComplete(() =>
            {
                if (NetworkManager && NetworkManager.IsClient)
                {
                    if (_bossPhase1.transform.parent.TryGetComponent(out NetworkObject nObj))
                    {
                        nObj.Despawn();
                    }
                }
                else
                {
                    Destroy(_bossPhase1.transform.parent.gameObject);
                }

                _bossPhase2.transform.parent.gameObject.SetActive(true);
            });
    }
}