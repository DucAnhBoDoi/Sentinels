using UnityEngine;

[RequireComponent(typeof(BossBehaviorManager))]
public class BossShockWaveAttackBehavior : BehaviorTreeNode
{
    [SerializeField]
    private BossShockWave _shockWave;

    private void Awake()
    {
        _shockWave.OnShockWaveFinished += OnShockWaveFinished;
    }

    private void OnShockWaveFinished()
    {
        BehaviorStatus = TaskStatus.Success;
    }

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        _shockWave.gameObject.SetActive(true);
    }
}
