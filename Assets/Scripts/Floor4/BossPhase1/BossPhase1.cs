using CleverCrow.Fluid.BTs.Trees;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.Events;

[RequireComponent(typeof(HealthManager), typeof(Animator), typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(NetworkAnimator))]
public class BossPhase1 : NetworkBehaviour, IDamagable
{
    private static readonly int Death = Animator.StringToHash("T_Death");
    private static readonly int Attack = Animator.StringToHash("T_Attack");
    private static readonly int FMoveThreshold = Animator.StringToHash("F_MoveThreshold");
    private static readonly int Hurt = Animator.StringToHash("T_Hurt");

    public UnityAction OnDeath;

    [SerializeField, HideInInspector] private HealthManager _hm;
    [SerializeField, HideInInspector] private NetworkAnimator _anim;
    [SerializeField, HideInInspector] private Collider2D _collider;
    [SerializeField, HideInInspector] private Rigidbody2D _rb;
    [SerializeField, HideInInspector] private SpriteRenderer _sr;

    [SerializeField] private BossProjectile _projectile;
    [SerializeField] private bool _spriteFacingRight;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _hitDistance;
    [SerializeField] private DamageHitBox _atkHitBox;
    [SerializeField] private CheckHitable _checkHitable;
    [SerializeField] private float _atkDuration;
    [SerializeField] private float _hurtDuration;
    [SerializeField] private GameObject _firewall;
    [SerializeField] private Transform _teleportPool;
    [SerializeField] private BehaviorTree _tree;

    private static GameObject[] _players;
    private static GameObject _targetedPlayer;
    private bool _isDead;
    private bool _isHurt;
    private float _hurtTimer;
    private float _hitBoxXPos;
    private Transform _bossTransform;

    private void Awake()
    {
        _bossTransform = transform.parent;
        _tree = new BehaviorTreeBuilder(gameObject)
            .Selector()
            .Sequence()
            .Condition(() => _checkHitable.Attackable)
            .Do(nameof(AttackBehavior), AttackBehavior)
            .WaitTime(_atkDuration)
            .End()
            .SelectorRandom()
            .Sequence()
            .Selector()
            .Sequence()
            .RandomChance(1, 5)
            .Do(nameof(TeleportToClosest), TeleportToClosest)
            .End()
            .Do(nameof(MoveTowardPlayerBehavior), MoveTowardPlayerBehavior)
            .End()
            .Selector()
            .Sequence()
            .Condition(() => _checkHitable.Attackable)
            .Do(nameof(AttackBehavior), AttackBehavior)
            .WaitTime(_atkDuration)
            .End()
            .Sequence()
            .Do(nameof(SpawnProjectileBehavior), SpawnProjectileBehavior)
            .WaitTime()
            .End()
            .End()
            .End()
            .Sequence()
            .Do(nameof(SpawnProjectileBehavior), SpawnProjectileBehavior)
            .WaitTime()
            .End()
            .End()
            .End()
            .Build();
    }

    private TaskStatus TeleportToClosest()
    {
        if (_teleportPool == null)
        {
            return TaskStatus.Failure;
        }

        Transform closest = _teleportPool.GetChild(0);

        foreach (Transform point in _teleportPool)
        {
            if (Vector2.Distance(point.position, _targetedPlayer.transform.position) <
                Vector2.Distance(closest.position, _targetedPlayer.transform.position))
            {
                closest = point;
            }
        }

        _bossTransform.position = closest.position;

        return TaskStatus.Success;
    }

    private void Start()
    {
        if (_firewall != null)
        {
            _firewall.SetActive(true);
        }

        _players = GameObject.FindGameObjectsWithTag("Player");
        _atkHitBox.gameObject.SetActive(false);
        _checkHitable.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (NetworkManager && !IsServer)
        {
            return;
        }

        if (_isDead)
        {
            return;
        }

        _bossTransform.position = transform.position;
        transform.localPosition = Vector2.zero;

        _rb.linearVelocity = Vector2.zero;

        foreach (var player in _players)
        {
            if (_targetedPlayer == null ||
                Vector2.Distance(player.transform.position, _bossTransform.position) <
                Vector2.Distance(_targetedPlayer.transform.position, _bossTransform.position))
            {
                _targetedPlayer = player;
            }
        }

        if (_isHurt)
        {
            _hurtTimer -= Time.deltaTime;
            if (_hurtTimer <= 0)
            {
                _isHurt = false;
            }
            else
            {
                return;
            }
        }

        _tree.Tick();
    }

    public void TakeDamage()
    {
        if (_hm.Health > 0)
        {
            _hm.ReduceHealth(1);
            if (!_isHurt)
            {
                _anim.Animator.SetTrigger(Hurt);
                _isHurt = true;
                _hurtTimer = _hurtDuration;
            }

            return;
        }

        if (_isDead)
        {
            return;
        }

        _anim.Animator.SetTrigger(Death);
        _collider.enabled = false;
        _atkHitBox.gameObject.SetActive(false);
        _checkHitable.gameObject.SetActive(false);
        _isDead = true;
        if (_firewall != null)
        {
            _firewall.gameObject.SetActive(false);
        }

        OnDeath?.Invoke();
    }

    private void HandleAttack()
    {
        _atkHitBox.gameObject.SetActive(true);
    }

    private TaskStatus AttackBehavior()
    {
        _anim.Animator.SetTrigger(Attack);
        return TaskStatus.Success;
    }

    private TaskStatus MoveTowardPlayerBehavior()
    {
        if (_targetedPlayer == null)
        {
            return TaskStatus.Failure;
        }

        Vector3 targetPosition = Vector2.MoveTowards(
            _bossTransform.position,
            _targetedPlayer.transform.position,
            _movementSpeed * Time.deltaTime);

        float offsetX = targetPosition.x - _bossTransform.position.x;
        float offsetY = targetPosition.y - _bossTransform.position.y;

        float moveThreshold = offsetX;
        if (moveThreshold == 0)
        {
            moveThreshold = offsetY;
        }

        _anim.Animator.SetFloat(FMoveThreshold, moveThreshold);

        float atkHitBoxX = _atkHitBox.transform.localPosition.x;
        float atkHitBoxY = _atkHitBox.transform.localPosition.y;
        float checkHitableX = _checkHitable.transform.localPosition.x;
        float checkHitableY = _checkHitable.transform.localPosition.y;

        if (NetworkManager && NetworkManager.IsServer)
        {
            if (offsetX > 0)
            {
                _atkHitBox.transform.localPosition = new Vector3(Mathf.Abs(atkHitBoxX), atkHitBoxY);
                _checkHitable.transform.localPosition = new Vector3(Mathf.Abs(checkHitableX), checkHitableY);
            }
            else if (offsetX < 0)
            {
                _atkHitBox.transform.localPosition = new Vector3(-Mathf.Abs(atkHitBoxX), atkHitBoxY);
                _checkHitable.transform.localPosition = new Vector3(-Mathf.Abs(checkHitableX), checkHitableY);
            }

            if (IsSpawned)
            {
                FlipBossClientRpc(offsetX);
            }
        }

        if (Vector2.Distance(_bossTransform.position, _targetedPlayer.transform.position) <= _hitDistance)
        {
            return TaskStatus.Success;
        }

        Debug.Log(_targetedPlayer.transform.position);
        _bossTransform.position = targetPosition;

        return TaskStatus.Continue;
    }

    [ClientRpc]
    private void FlipBossClientRpc(float offsetX)
    {
        if (offsetX > 0)
        {
            Debug.Log("flip");
            _sr.flipX = !_spriteFacingRight;
        }
        else if (offsetX < 0)
        {
            Debug.Log("no flip");
            _sr.flipX = _spriteFacingRight;
        }
    }

    private TaskStatus SpawnProjectileBehavior()
    {
        if (_projectile == null)
        {
            return TaskStatus.Failure;
        }

        if (!NetworkManager)
        {
            BossProjectile projectile = Instantiate(_projectile);
            projectile.transform.position = _bossTransform.position;
            projectile.Target = _targetedPlayer.transform;
        }
        else if (NetworkManager.IsServer)
        {
            BossProjectile projectile = Instantiate(_projectile);
            projectile.NetworkObject.Spawn(true);
            projectile.transform.position = _bossTransform.position;
            projectile.Target = _targetedPlayer.transform;
        }

        return TaskStatus.Success;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_hm == null)
        {
            _hm = GetComponent<HealthManager>();
        }

        if (_anim == null)
        {
            _anim = GetComponent<NetworkAnimator>();
        }

        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }

        if (_rb == null)
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        if (_sr == null)
        {
            _sr = GetComponent<SpriteRenderer>();
        }
    }
#endif
}