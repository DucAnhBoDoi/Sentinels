using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckHitable : MonoBehaviour
{
    [SerializeField, HideInInspector] private Collider2D _collider;

    public bool Attackable { get; private set; }

    private List<GameObject> _targets;

    private void Awake()
    {
        _targets = new List<GameObject>();
    }

    private void Update()
    {
        Attackable = false;
        foreach (GameObject target in _targets)
        {
            if (target.CompareTag("Player"))
            {
                Attackable = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _targets.Add(other.gameObject);

        Debug.Log($"In range to attack {other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _targets.Remove(other.gameObject);

        Debug.Log($"Out of range to attack {other.name}");
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