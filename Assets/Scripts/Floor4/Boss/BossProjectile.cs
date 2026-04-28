using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossProjectile : NetworkBehaviour
{
    [SerializeField, HideInInspector] private Collider2D _collider;

    [HideInInspector] public Transform Target;

    public float ProjectileVelocity;

    public float LifeTime;

    private void Start()
    {
        if (Target == null)
        {
            if (NetworkManager && NetworkManager.IsServer)
            {
                NetworkObject.Despawn();
            }
            else if (!NetworkManager)
            {
                Destroy(gameObject);
            }
        }
    }

    private void Update()
    {
        if (NetworkManager && !NetworkManager.IsServer)
        {
            return;
        }

        LifeTime -= Time.deltaTime;
        if (LifeTime <= 0)
        {
            if (NetworkManager && NetworkManager.IsServer)
            {
                NetworkObject.Despawn();
            }
            else if (!NetworkManager)
            {
                Destroy(gameObject);
            }

            return;
        }

        float step = ProjectileVelocity * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, Target.position, step);
        Vector2 direction = Target.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new(0, 0, angle));

        if (Vector2.Distance(transform.position, Target.position) <= 1)
        {
            transform.position = Target.position + Vector3.down * 2;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerHit(other);
    }

    private void HandlePlayerHit(Collider2D player)
    {
        if (!player.CompareTag("Player"))
        {
            return;
        }

        if (player.gameObject.TryGetComponent(out PlayerHP playerHp))
        {
            playerHp.TakeDamage(1);
        }

        if (!NetworkManager || NetworkManager.IsServer)
        {
            Destroy(gameObject);
        }
        else
        {
            NetworkObject.Despawn();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        _collider.isTrigger = true;
    }
#endif
}