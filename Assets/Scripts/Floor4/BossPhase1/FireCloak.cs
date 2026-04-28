using System;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem), typeof(Collider2D))]
public class FireCloak : MonoBehaviour
{
    [SerializeField, HideInInspector] private ParticleSystem _ps;
    [SerializeField, HideInInspector] private Collider2D _collider;

    [SerializeField] private float _pulseDuration;

    private float _pulseTimer;

    private void Awake()
    {
        _pulseTimer = _pulseDuration;
    }

    private void Update()
    {
        _pulseTimer -= Time.deltaTime;
        if (_pulseTimer <= 0)
        {
            if (_ps.isPlaying)
            {
                _ps.Stop();
                _collider.enabled = false;
            }
            else
            {
                _ps.Play();
                _collider.enabled = true;
            }

            _pulseTimer = _pulseDuration;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.gameObject.TryGetComponent(out PlayerHP playerHp))
        {
            playerHp.TakeDamage(1);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_ps == null)
        {
            _ps = GetComponent<ParticleSystem>();
        }

        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        _collider.isTrigger = true;
    }
#endif
}