using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(BossPunchBehavior), typeof(BossTargetPlayerBehavior), typeof(BossMoveToPlayerBehavior))]
[RequireComponent(typeof(BossShockWaveAttackBehavior))]
public class BossBehaviorManager : MonoBehaviour
{
    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
    }

    public PlayerController TargetedPlayer;

    private BossController _controller;
    private BossPunchBehavior _punchBehavior;
    private BossTargetPlayerBehavior _targetPlayerBehavior;
    private BossMoveToPlayerBehavior _moveToPlayerBehavior;
    private BossShockWaveAttackBehavior _shockWaveAttackBehavior;

    private Light2D _globalLight;
    private BossPhase _phase;

    private void Awake()
    {
        _controller = GetComponent<BossController>();
        _punchBehavior = GetComponent<BossPunchBehavior>();
        _targetPlayerBehavior = GetComponent<BossTargetPlayerBehavior>();
        _moveToPlayerBehavior = GetComponent<BossMoveToPlayerBehavior>();
        _shockWaveAttackBehavior = GetComponent<BossShockWaveAttackBehavior>();
    }

    private void Start()
    {
        StartCoroutine(RootBehavior());
    }

    private IEnumerator RootBehavior()
    {
        while (true)
        {
            yield return new WaitForSeconds(_controller.Stats.RecoverTime);
            switch (_phase)
            {
                case BossPhase.Phase1:
                    {
                        yield return Phase1();
                    }
                    break;
                case BossPhase.Phase2:
                    {
                        yield return Phase2();
                    }
                    break;
                case BossPhase.Phase3:
                    {
                        yield return Phase3();
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private IEnumerator Phase1()
    {
        float distance = Vector2.Distance(transform.position, TargetedPlayer.transform.position);
        if (distance <= 9)
        {
            int rng = Random.Range(1, 11);
            if (rng <= 3)
            {
                yield return ShockWaveAttack();
            }
            else if (distance < 6)
            {
                yield return PunchAttack();
            }
            else
            {
                yield return ChargeAttack();
            }
        }
        else if (distance <= 40)
        {
            yield return ChargeAttack();
        }
        else
        {
            yield return ChargeAttack();
        }
    }

    private IEnumerator Phase2()
    {
        yield break;
    }

    private IEnumerator Phase3()
    {
        yield break;
    }

    private IEnumerator ChargeAttack()
    {
        yield return StartBehavior(_targetPlayerBehavior);
        yield return StartBehavior(_moveToPlayerBehavior);
    }

    private IEnumerator PunchAttack()
    {
        yield return StartBehavior(_targetPlayerBehavior);
        yield return StartBehavior(_punchBehavior);
    }

    private IEnumerator ShockWaveAttack()
    {
        yield return transform.DOShakePosition(0.2f).WaitForCompletion();
        yield return StartBehavior(_shockWaveAttackBehavior);
    }

    private IEnumerator StartBehavior(BehaviorTreeNode node)
    {
        node.BehaviorStart();
        while (node.BehaviorStatus == BehaviorTreeNode.TaskStatus.Running)
        {
            yield return null;
        }
        node.BehaviorEnd();
    }
}
