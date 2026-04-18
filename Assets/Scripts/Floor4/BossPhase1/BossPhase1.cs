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

    [SerializeField] private bool _spriteFacingRight;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _hitDistance;
    [SerializeField] private Collider2D _atkHitBox;
    [SerializeField] private float _atkDuration;
    [SerializeField] private BehaviorTree _tree;

    private GameObject[] _players;
    private GameObject _targetedPlayer;
    private bool _isDead;

    private void Awake()
    {
        _atkHitBox.gameObject.SetActive(false);
        _tree = new BehaviorTreeBuilder(gameObject)
            .Sequence()
            .Do(nameof(MoveTowardPlayerBehavior), MoveTowardPlayerBehavior)
            .Do(nameof(AttackBehavior), AttackBehavior)
            .WaitTime(_atkDuration)
            .End()
            .Build();
    }

    private void Start()
    {
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
        _hm.ReduceHealth(1);

        if (_hm.Health > 0) return;
        _anim.SetTrigger(Death);
        _collider.enabled = false;
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
        if (Vector2.Distance(_targetedPlayer.transform.position, transform.position) <= _hitDistance)
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

        Debug.Log(moveThreshold);

        _anim.SetFloat(FMoveThreshold, moveThreshold);

        if (offsetX > 0)
        {
            _sr.flipX = !_spriteFacingRight;
        }
        else if (offsetX < 0)
        {
            _sr.flipX = _spriteFacingRight;
        }

        transform.position = targetPosition;

        return TaskStatus.Continue;
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