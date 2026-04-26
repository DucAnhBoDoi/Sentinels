using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageHitBox : MonoBehaviour
{
    [SerializeField, HideInInspector] private Collider2D _collider;

    [SerializeField] private float _enableDuration;

    private float _timer;

    private void OnEnable()
    {
        _timer = _enableDuration;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"Boss hit player {other.name}");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_collider == null)
        {
            _collider = GetComponent<Collider2D>();
        }
        
        _collider.isTrigger = true;
    }
#endif
}