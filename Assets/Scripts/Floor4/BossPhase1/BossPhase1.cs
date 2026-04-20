using CleverCrow.Fluid.BTs.Trees;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;

[RequireComponent(typeof(HealthManager), typeof(Animator), typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class BossPhase1 : MonoBehaviour, IDamagable
{
    private static readonly int Death = Animator.StringToHash("T_Death");
    private static readonly int Attack = Animator.StringToHash("T_Attack");
    private static readonly int FMoveThreshold = Animator.StringToHash("F_MoveThreshold");

    [SerializeField, HideInInspector] private HealthManager _hm;
    [SerializeField, HideInInspector] private Animator _anim;
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
    [SerializeField] private BehaviorTree _tree;

    private GameObject[] _players;
    private GameObject _targetedPlayer;
    private bool _isDead;
    private float _hitBoxXPos;

    private void Awake()
    {
        _tree = new BehaviorTreeBuilder(gameObject)
            .SelectorRandom()
            .Sequence()
            .Do(nameof(MoveTowardPlayerBehavior), MoveTowardPlayerBehavior)
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
            .Build();
    }

    private void Start()
    {
        _atkHitBox.gameObject.SetActive(false);
        _checkHitable.gameObject.SetActive(true);
        _players = GameObject.FindGameObjectsWithTag("Player");
        if (_players.Length > 0 && _targetedPlayer == null)
        {
            _targetedPlayer = _players[0];
        }
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        _rb.linearVelocity = Vector2.zero;

        if (_targetedPlayer)
        {
            foreach (var player in _players)
            {
                if (Vector2.Distance(player.transform.position, transform.position) <
                    Vector2.Distance(_targetedPlayer.transform.position, transform.position))
                {
                    _targetedPlayer = player;
                }
            }
        }

        _tree.Tick();
    }

    public void TakeDamage()
    {
        if (_hm.Health > 0)
        {
            _hm.ReduceHealth(1);
            return;
        }

        if (_isDead)
        {
            return;
        }

        _anim.SetTrigger(Death);
        _collider.enabled = false;
        _atkHitBox.gameObject.SetActive(false);
        _checkHitable.gameObject.SetActive(false);
        _isDead = true;
    }

    private void HandleAttack()
    {
        _atkHitBox.gameObject.SetActive(true);
    }

    private TaskStatus AttackBehavior()
    {
        _anim.SetTrigger(Attack);
        return TaskStatus.Success;
    }

    private TaskStatus MoveTowardPlayerBehavior()
    {
        Debug.Log(Vector2.Distance(transform.position, _targetedPlayer.transform.position));
        if (Vector2.Distance(transform.position, _targetedPlayer.transform.position) <= _hitDistance)
        {
            return TaskStatus.Success;
        }

        Vector3 targetPosition = Vector2.MoveTowards(
            transform.position,
            _targetedPlayer.transform.position,
            _movementSpeed * Time.deltaTime);

        float offsetX = targetPosition.x - transform.position.x;
        float offsetY = targetPosition.y - transform.position.y;

        float moveThreshold = offsetX;
        if (moveThreshold == 0)
        {
            moveThreshold = offsetY;
        }

        _anim.SetFloat(FMoveThreshold, moveThreshold);

        float atkHitBoxX = _atkHitBox.transform.localPosition.x;
        float atkHitBoxY = _atkHitBox.transform.localPosition.y;
        float checkHitableX = _checkHitable.transform.localPosition.x;
        float checkHitableY = _checkHitable.transform.localPosition.y;

        if (offsetX > 0)
        {
            _sr.flipX = !_spriteFacingRight;
            _atkHitBox.transform.localPosition = new Vector3(Mathf.Abs(atkHitBoxX), atkHitBoxY);
            _checkHitable.transform.localPosition = new Vector3(Mathf.Abs(checkHitableX), checkHitableY);
        }
        else if (offsetX < 0)
        {
            _sr.flipX = _spriteFacingRight;
            _atkHitBox.transform.localPosition = new Vector3(-Mathf.Abs(atkHitBoxX), atkHitBoxY);
            _checkHitable.transform.localPosition = new Vector3(-Mathf.Abs(checkHitableX), checkHitableY);
        }

        transform.position = targetPosition;

        return TaskStatus.Continue;
    }

    private TaskStatus SpawnProjectileBehavior()
    {
        BossProjectile projectile = Instantiate(_projectile);
        projectile.Target = _targetedPlayer.transform;
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
            _anim = GetComponent<Animator>();
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