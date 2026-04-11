using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [field: SerializeField]
    public int Health { get; private set; }

    public void ReduceHealth(int amount)
    {
        if (Health > 0)
        {
            Health = Mathf.Max(0, Health - amount);
        }
    }

    public void SetHealthUp(int amount)
    {
        Health = Mathf.Max(Health, amount);
    }
}
