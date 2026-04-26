using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckHitable : MonoBehaviour
{
    [SerializeField, HideInInspector] private Collider2D _collider;

    public bool Attackable { get; private set; }

    private int _count;

    private void Update()
    {
        Attackable = _count > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"In range to attack {other.name}");

        _count++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Debug.Log($"Out of range to attack {other.name}");

        _count--;
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