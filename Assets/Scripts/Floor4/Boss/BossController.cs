using UnityEngine;

[RequireComponent(typeof(BossControlColor), typeof(BossBehaviorManager), typeof(HealthManager))]
public class BossController : MonoBehaviour, IDamagable
{
    [field: SerializeField]
    public BossStatsSO Stats { get; private set; }

    [field: SerializeField]
    public SpriteRenderer BossBodySr { get; private set; }

    public BossControlColor BossColor { get; private set; }

    public HealthManager BossHealth { get; private set; }

    private void Awake()
    {
        BossColor = GetComponent<BossControlColor>();
        BossHealth = GetComponent<HealthManager>();
        BossHealth.SetHealthUp(Stats.BossHealth);
    }

    public void TakeDamage()
    {
        BossHealth.ReduceHealth(1);
    }
}
