using UnityEngine;

[CreateAssetMenu(fileName = "BossStatsSO", menuName = "Scriptable Objects/BossStatsSO")]
public class BossStatsSO : ScriptableObject
{
    [field: SerializeField]
    public float BlowUpTime { get; private set; }

    [field: SerializeField]
    public float FlashTime { get; private set; }

    [field: SerializeField]
    public float SwitchColorTime { get; private set; }

    [field: SerializeField]
    public float RecoverTime { get; private set; }

    [field: SerializeField]
    public float ProjectileVelocity { get; private set; }

    [field: SerializeField]
    public float ProjectileLifeTime { get; private set; }

    [SerializeField]
    private PlayerStatsSO _playerStats;

    public Color[] BossColors => _playerStats.FlashLightColors;
}
