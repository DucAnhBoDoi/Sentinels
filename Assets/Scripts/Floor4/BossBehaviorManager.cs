using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(BossPunchBehavior), typeof(BossTargetPlayerBehavior), typeof(BossMoveToPlayerBehavior))]
[RequireComponent(typeof(BossShockWaveAttackBehavior), typeof(BossShootBehavior))]
public class BossBehaviorManager : MonoBehaviour
{
    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
    }

    [HideInInspector]
    public PlayerController TargetedPlayer;

    public PlayerController[] Players { get; private set; }

    [SerializeField]
    private Light2D _globalLight;

    private BossController _controller;
    private BossPunchBehavior _punchBehavior;
    private BossTargetPlayerBehavior _targetPlayerBehavior;
    private BossMoveToPlayerBehavior _moveToPlayerBehavior;
    private BossShockWaveAttackBehavior _shockWaveAttackBehavior;
    private BossShootBehavior _shootBehavior;

    private BossPhase _phase;

    private void Awake()
    {
        _controller = GetComponent<BossController>();
        _punchBehavior = GetComponent<BossPunchBehavior>();
        _targetPlayerBehavior = GetComponent<BossTargetPlayerBehavior>();
        _moveToPlayerBehavior = GetComponent<BossMoveToPlayerBehavior>();
        _shockWaveAttackBehavior = GetComponent<BossShockWaveAttackBehavior>();
        _shootBehavior = GetComponent<BossShootBehavior>();
    }

    private void Start()
    {
        Players = GameObject
            .FindGameObjectsWithTag("Player")
            .Select(go => go.GetComponent<PlayerController>()).ToArray();
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
        if (TargetedPlayer == null)
        {
            yield return StartBehavior(_targetPlayerBehavior);
        }
        float distance = Vector2.Distance(transform.position, TargetedPlayer.transform.position);
        int rng = Random.Range(1, 11);
        if (distance <= 9)
        {
            if (rng <= 3)
            {
                yield return ShockWaveAttack();
            }
            else if (distance < 6)
            {
                yield return PunchAttack();
            }
            else if (rng <= 9)
            {
                yield return ChargeAttack();
            }
            else
            {
                yield return ShootAttack();
            }
        }
        else if (distance <= 40)
        {
            if (rng <= 3)
            {
                yield return ShootAttack();
            }
            else
            {
                yield return ChargeAttack();
            }
        }
        else
        {
            if (rng <= 3)
            {
                yield return ChargeAttack();
            }
            else
            {
                yield return ShootAttack();
            }
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

    private IEnumerator ShootAttack()
    {
        yield return StartBehavior(_targetPlayerBehavior);
        yield return StartBehavior(_shootBehavior);
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
