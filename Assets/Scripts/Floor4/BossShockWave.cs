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
    }
}
