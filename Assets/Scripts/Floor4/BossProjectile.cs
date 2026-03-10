using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    [HideInInspector]
    public Transform Target;

    [HideInInspector]
    public float ProjectileVelocity;

    [HideInInspector]
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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(nameof(GameLayerMask.Player)))
        {
            Vector3 direction = other.transform.position - transform.position;
            if (other.gameObject.TryGetComponent(out KnockBackManager knock))
            {
                knock.KnockBack(direction, 20);
            }
            Destroy(gameObject);
        }
    }
}
