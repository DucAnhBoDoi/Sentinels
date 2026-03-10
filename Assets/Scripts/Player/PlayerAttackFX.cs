using DG.Tweening;
using UnityEngine;

public class PlayerAttackFX : MonoBehaviour
{
    [SerializeField]
    private Transform _rotateAxis;

    private void OnEnable()
    {
        _rotateAxis
            .DOLocalRotate(Vector3.back * 90, 0.2f)
            .SetTarget(this)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        _rotateAxis.localRotation = Quaternion.Euler(0, 0, 0);
    }
}
