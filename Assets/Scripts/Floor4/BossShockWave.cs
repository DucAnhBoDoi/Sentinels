using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class BossShockWave : MonoBehaviour
{
    public event UnityAction OnShockWaveStarted;
    public event UnityAction OnShockWaveFinished;

    [SerializeField]
    private float _scaleTarget;

    [SerializeField]
    private float _duration;

    private HashSet<Collider2D> _interacted;

    private void Awake()
    {
        _interacted = new();
    }

    private void OnEnable()
    {
        OnShockWaveStarted?.Invoke();
        transform
            .DOScale(_scaleTarget, _duration)
            .SetTarget(this)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnShockWaveFinished?.Invoke();
            });
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        transform.localScale = Vector3.one;
        _interacted.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_interacted.Contains(other) && other.gameObject.TryGetComponent(out KnockBackManager knock))
        {
            Vector3 direction = other.transform.position - transform.position;
            knock.KnockBack(direction, 20);
            _interacted.Add(other);
        }
    }
}
