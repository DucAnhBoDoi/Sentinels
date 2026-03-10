using DG.Tweening;
using UnityEngine;

public class BossPunchBehavior : BehaviorTreeNode
{
    [SerializeField]
    private Transform _bossHand;

    private void OnDestroy()
    {
        DOTween.Kill(this);
    }

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        DOTween.Sequence(this)
            .Append(_bossHand.DOLocalMoveX(0.7f, 0.2f))
            .Append(_bossHand.DOLocalMoveX(0.1f, 0.2f))
            .OnComplete(() => BehaviorStatus = TaskStatus.Success);
    }
}
