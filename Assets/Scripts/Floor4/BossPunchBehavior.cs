using DG.Tweening;
using UnityEngine;

public class BossPunchBehavior : BehaviorTreeNode
{
    [SerializeField]
    private Transform _bossHand;

    [SerializeField]
    private Transform _rayPointContainer;

    private void OnDestroy()
    {
        DOTween.Kill(this);
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

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        _bossHand
            .DOLocalMoveX(0.7f, 0.2f)
            .OnKill(() => _bossHand.DOLocalMoveX(0.1f, 0.2f))
            .OnComplete(() => BehaviorStatus = TaskStatus.Success);
    }
}
