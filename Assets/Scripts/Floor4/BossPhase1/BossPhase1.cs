using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HealthManager), typeof(Animator), typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossPhase1 : MonoBehaviour, IDamagable
{
    private static readonly int Death = Animator.StringToHash("T_Death");

    [SerializeField, HideInInspector] private HealthManager _hm;
    [SerializeField, HideInInspector] private Animator _anim;
    [SerializeField, HideInInspector] private Collider2D _collider;
    [SerializeField, HideInInspector] private Rigidbody2D _rb;

    [SerializeField] private Collider2D _atkHitBox;

    private void Update()
    {
        _rb.linearVelocity = Vector2.zero;
    }

    public void TakeDamage()
    {
        _hm.ReduceHealth(1);

        if (_hm.Health > 0) return;
        _anim.SetTrigger(Death);
        _collider.enabled = false;
    }

    private void HandleBossAttack()
    {
        _atkHitBox.gameObject.SetActive(true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_hm == null)
        {
            _hm = GetComponent<HealthManager>();
        }

        if (_anim == null)
        {
            _anim = GetComponent<Animator>();
        }

        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
        }
    }
#endif
}