using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BossBehaviorManager))]
public class BossMoveToPlayerBehavior : BehaviorTreeNode
{
    private BossBehaviorManager _boss;

    private void Awake()
    {
        _boss = GetComponent<BossBehaviorManager>();
    }

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        Vector3 direction = _boss.TargetedPlayer.transform.position - transform.position;
        transform
            .DOMove(_boss.TargetedPlayer.transform.position + direction.normalized * 4, 0.7f)
            .SetTarget(this)
            .OnComplete(() => BehaviorStatus = TaskStatus.Success);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(nameof(GameLayerMask.Wall)))
        {
            DOTween.Kill(this);
            BehaviorStatus = TaskStatus.Success;
        }
    }
}
