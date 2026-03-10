using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerKnockBack : MonoBehaviour
{
    private PlayerController _controller;

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        _controller.KnockManager.OnKnockBack += OnPlayerKnockBack;
        _controller.KnockManager.OnKnockBackRecover += OnPlayerKnockBackRecover;
    }

    private void OnPlayerKnockBack()
    {
        _controller.State = PlayerController.PlayerState.KnockBack;
        _controller.HurtAnimation();
    }

    private void OnPlayerKnockBackRecover()
    {
        _controller.State = PlayerController.PlayerState.Idle;
    }
}
