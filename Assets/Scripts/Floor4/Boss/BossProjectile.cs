using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossProjectile : MonoBehaviour
{
    [SerializeField, HideInInspector] private Collider2D _collider;

    [HideInInspector] public Transform Target;

    public float ProjectileVelocity;

    public float LifeTime;

    private void Start()
    {
        if (Target == null)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        LifeTime -= Time.deltaTime;
        if (LifeTime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        float step = ProjectileVelocity * Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, Target.position, step);
        Vector2 direction = Target.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new(0, 0, angle));

        if (Vector2.Distance(transform.position, Target.position) <= 0.1f)
        {
            Collider2D col = Physics2D.OverlapCircle(transform.position, 5, LayerMask.GetMask("Player"));
            HandlePlayerHit(col);
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

        // Vector3 direction = other.transform.position - transform.position;
        // if (other.gameObject.TryGetComponent(out KnockBackManager knock))
        // {
        //     knock.KnockBack(direction, 20);
        // }

        Debug.Log($"Projectile hit player {player.name}");

        Destroy(gameObject);
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