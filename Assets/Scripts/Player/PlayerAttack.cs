using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField]
    private PlayerAttackFX _attackFX;

    private PlayerController _controller;

    private float _cooldownTimer;

    private bool _attackReady;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _attackFX.Damage = _controller.Stats.AttackDamage;
        _attackReady = true;
    }

    private void Update()
    {
        if (_attackReady && !_controller.FlashLight.isActiveAndEnabled)
        {
            if (_controller.Input.Player.Attack.WasPerformedThisFrame())
            {
                _attackFX.gameObject.SetActive(true);
                _attackReady = false;
                _cooldownTimer = _controller.Stats.AttackCoolDown;
            }
        }
        else
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0)
            {
                _attackReady = true;
            }
        }
    }
}
