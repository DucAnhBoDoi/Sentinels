using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class KnockBackManager : MonoBehaviour
{
    public event UnityAction OnKnockBack;
    public event UnityAction OnKnockBackRecover;

    public float KnockBackRecoverTime;

    private Rigidbody2D _rb;

    private float _recoverTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_recoverTimer <= 0)
        {
            return;
        }

        _recoverTimer -= Time.deltaTime;
        if (_recoverTimer <= 0)
        {
            OnKnockBackRecover?.Invoke();
        }
    }

    public void KnockBack(Vector3 direction, float knockBackStrength)
    {
        _recoverTimer = KnockBackRecoverTime;
        OnKnockBack?.Invoke();
        _rb.linearVelocity = direction.normalized * knockBackStrength;
    }
}
