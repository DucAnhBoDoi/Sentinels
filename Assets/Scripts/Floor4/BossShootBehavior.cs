using UnityEngine;

[RequireComponent(typeof(BossBehaviorManager), typeof(BossController))]
public class BossShootBehavior : BehaviorTreeNode
{
    [SerializeField]
    private BossProjectile _projectilePrefab;

    private BossBehaviorManager _boss;
    private BossController _controller;

    private void Awake()
    {
        _boss = GetComponent<BossBehaviorManager>();
        _controller = GetComponent<BossController>();
    }

    public override void BehaviorStart()
    {
        base.BehaviorStart();
        BossProjectile projectile = Instantiate(_projectilePrefab);
        projectile.Target = _boss.TargetedPlayer.transform;
        projectile.LifeTime = _controller.Stats.ProjectileLifeTime;
        projectile.ProjectileVelocity = _controller.Stats.ProjectileVelocity;
        projectile.transform.position = transform.position;
        BehaviorStatus = TaskStatus.Success;
    }
}
