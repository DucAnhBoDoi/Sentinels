using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(BossController))]
[RequireComponent(typeof(BossPunchBehavior), typeof(BossTargetPlayerBehavior), typeof(BossMoveToPlayerBehavior))]
[RequireComponent(typeof(BossShockWaveAttackBehavior), typeof(BossShootBehavior), typeof(BossTeleportBehavior))]
public class BossBehaviorManager : MonoBehaviour
{
    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
    }

    [HideInInspector] public GameObject TargetedPlayer;

    public GameObject[] Players { get; private set; }

    [SerializeField] private Light2D _globalLight;

    private BossController _controller;
    private BossPunchBehavior _punchBehavior;
    private BossTargetPlayerBehavior _targetPlayerBehavior;
    private BossMoveToPlayerBehavior _moveToPlayerBehavior;
    private BossShockWaveAttackBehavior _shockWaveAttackBehavior;
    private BossShootBehavior _shootBehavior;
    private BossTeleportBehavior _teleportBehavior;

    private BossPhase _phase;

    private bool _isDead;
    private Coroutine _behaviorRoutine;

    private int _p2HideCount;
    private bool _p2AllowAttack;

    private void Awake()
    {
        _controller = GetComponent<BossController>();
        _punchBehavior = GetComponent<BossPunchBehavior>();
        _targetPlayerBehavior = GetComponent<BossTargetPlayerBehavior>();
        _moveToPlayerBehavior = GetComponent<BossMoveToPlayerBehavior>();
        _shockWaveAttackBehavior = GetComponent<BossShockWaveAttackBehavior>();
        _shootBehavior = GetComponent<BossShootBehavior>();
        _teleportBehavior = GetComponent<BossTeleportBehavior>();
    }

    private void Start()
    {
        Players = GameObject.FindGameObjectsWithTag("Player");
        _behaviorRoutine = StartCoroutine(RootBehavior());

        _controller.BossColor.OnColor += OnBossChangeColor;
        _controller.BossColor.OnNoColor += OnBossResetColor;
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        switch (_phase)
        {
            case BossPhase.Phase1:
            {
                if (_controller.BossHealth.Health <= _controller.Stats.BossHealth / 2)
                {
                    _phase = BossPhase.Phase2;
                    _controller.BossColor.enabled = true;
                    _controller.BossColor.TransitionToColorState();
                }
            }
                break;
            case BossPhase.Phase2:
                break;
            case BossPhase.Phase3:
                break;
            default:
                break;
        }

        if (_controller.BossHealth.Health <= 0)
        {
            OnDefeated();
        }
    }

    private IEnumerator RootBehavior()
    {
        while (true)
        {
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
        yield return new WaitForSeconds(_controller.Stats.RecoverTime);
        if (TargetedPlayer == null)
        {
            yield return StartBehavior(_targetPlayerBehavior);
        }

        float distance = Vector2.Distance(transform.position, TargetedPlayer.transform.position);
        int rng = Random.Range(1, 11);
        if (distance <= 7)
        {
            if (rng <= 3)
            {
                yield return ShockWaveAttack();
            }
            else
            {
                yield return PunchAttack();
            }
        }
        else if (distance <= 9)
        {
            if (rng <= 3)
            {
                yield return ShockWaveAttack();
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
        else if (distance <= 30)
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
        if (_p2HideCount > 0)
        {
            _globalLight.intensity = 0;
        }

        while (_p2HideCount >= 0)
        {
            yield return StartBehavior(_teleportBehavior);
            while (_controller.BossColor.IsSpotted)
            {
                yield return null;
            }

            int health = _controller.BossHealth.Health;
            while (_p2HideCount >= 0
                   && !_controller.BossColor.IsSpotted
                   && _controller.BossHealth.Health == health)
            {
                yield return null;
            }

            _p2HideCount--;
            _p2AllowAttack = _p2HideCount < 0;
            // if (_p2HideCount >= 0)
            // {
            //     yield return wait;
            // }
        }

        if (_p2AllowAttack)
        {
            yield return Phase1();
        }
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

    private void OnBossChangeColor()
    {
        _p2HideCount = 2;
        _p2AllowAttack = false;
    }

    private void OnBossResetColor(bool didBlowUp)
    {
        _globalLight.intensity = 1;
        _p2HideCount = -2;
        _p2AllowAttack = true;
        if (didBlowUp)
        {
            foreach (GameObject player in Players)
            {
                if (player.TryGetComponent(out PlayerController ctl))
                {
                    ctl.HurtAnimation();
                }
            }
        }
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

    private void OnDefeated()
    {
        if (_behaviorRoutine != null)
        {
            StopCoroutine(_behaviorRoutine);
        }

        _controller.BossColor.TransitionToNoColorState();
        _controller.BossColor.enabled = false;
        _isDead = true;
        _globalLight.intensity = 1;
        _controller.BossBodySr.color = Color.gray;
        foreach (GameObject player in Players)
        {
            if (player.TryGetComponent(out PlayerAttack atk))
            {
                atk.enabled = false;
            }
        }
    }
}