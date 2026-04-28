using System;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [field: SerializeField] public int Health { get; private set; }

    [SerializeField] private HealthBar _healthBar;

    private int _maxHealth;

    private void Start()
    {
        _maxHealth = Health;
    }

    public void ReduceHealth(int amount)
    {
        if (Health > 0)
        {
            Health = Mathf.Max(0, Health - amount);
            if (_healthBar != null)
            {
                _healthBar.UpdateBar(Health, _maxHealth);
            }
        }
    }

    public void SetHealthUp(int amount)
    {
        Health = Mathf.Max(Health, amount);
        if (_healthBar != null)
        {
            _healthBar.UpdateBar(Health, _maxHealth);
        }
    }
}