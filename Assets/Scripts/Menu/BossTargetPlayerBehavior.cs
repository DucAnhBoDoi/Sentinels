using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BossBehaviorManager))]
public class BossTargetPlayerBehavior : BehaviorTreeNode
{
    private BossBehaviorManager _boss;

    private void Awake()
    {
        _boss = GetComponent<BossBehaviorManager>();
    }

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        Vector2 direction = _boss.TargetedPlayer.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform
            .DORotate(Vector3.forward * angle, 0.2f)
            .OnComplete(() => BehaviorStatus = TaskStatus.Success);
    }
}
