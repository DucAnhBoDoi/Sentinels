using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem), typeof(Collider2D), typeof(NetworkAnimator))]
public class ElectricWall : NetworkBehaviour
{
    [SerializeField, HideInInspector] private ParticleSystem _ps;
    [SerializeField, HideInInspector] private Collider2D _collider;
    [SerializeField, HideInInspector] private NetworkAnimator _anim;

    [SerializeField] private float _pulseDuration;

    private float _pulseTimer;

    private void Awake()
    {
        _pulseTimer = _pulseDuration;
    }

    private void Update()
    {
        _pulseTimer -= Time.deltaTime;

        var shape = _ps.shape;
        shape.radius += 1;

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
                if (NetworkManager.Singleton && NetworkManager.Singleton.IsClient && IsSpawned)
                {
                    _anim.SetTrigger("T_ElectricWall");
                }
                else
                {
                    _anim.Animator.SetTrigger("T_ElectricWall");
                }

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

        if (_anim == null)
        {
            _anim = GetComponent<NetworkAnimator>();
        }

        _collider.isTrigger = true;
    }
#endif
}