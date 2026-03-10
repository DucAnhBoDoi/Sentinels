using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BossBehaviorManager))]
public class BossMoveToPlayerBehavior : BehaviorTreeNode
{
    [SerializeField]
    private Transform _rayPointContainer;

    private BossBehaviorManager _boss;

    private void Awake()
    {
        _boss = GetComponent<BossBehaviorManager>();
    }

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        Vector3 direction = _boss.TargetedPlayer.transform.position - transform.position;
        float distance = Mathf.Abs(Vector2.Distance(transform.position, _boss.TargetedPlayer.transform.position));
        float velocity = 35;
        if (distance <= 15)
        {
            velocity = 15;
        }
        float time = distance / velocity;
        transform
            .DOMove(_boss.TargetedPlayer.transform.position + direction.normalized * 4, time)
            .SetTarget(this)
            .OnComplete(() => BehaviorStatus = TaskStatus.Success);
    }

    private void FixedUpdate()
    {
        if (BehaviorStatus != TaskStatus.Running)
        {
            return;
        }

        Collider2D collider = null;

        foreach (Transform rayPoint in _rayPointContainer)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position,
                    (rayPoint.position - transform.position).normalized,
                    Vector2.Distance(rayPoint.position, transform.position),
                    LayerMask.GetMask(nameof(GameLayerMask.Player), nameof(GameLayerMask.Wall)));
            if (hit.collider != null)
            {
                collider = hit.collider;
                break;
            }
        }

        if (collider == null)
        {
            return;
        }

        DOTween.Kill(this);
        BehaviorStatus = TaskStatus.Success;
        Vector3 direction = collider.transform.position - transform.position;
        if (collider.gameObject.TryGetComponent(out KnockBackManager knock))
        {
            knock.KnockBack(direction, 20);
        }
    }
}
