using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsSO", menuName = "Scriptable Objects/PlayerStatsSO")]
public class PlayerStatsSO : ScriptableObject
{
    [field: SerializeField]
    public float MovementSpeed { get; private set; }

    [field: SerializeField]
    public Color[] FlashLightColors { get; private set; }

    [field: SerializeField]
    public int AttackDamage { get; private set; }

    [field: SerializeField]
    public float AttackCoolDown { get; private set; }

    [field: SerializeField]
    public float KnockBackRecoverTime { get; private set; }
}
