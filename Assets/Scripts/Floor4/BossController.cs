using UnityEngine;

[RequireComponent(typeof(BossControlColor), typeof(BossBehaviorManager))]
public class BossController : MonoBehaviour
{
    [field: SerializeField]
    public BossStatsSO Stats { get; private set; }

    [field: SerializeField]
    public SpriteRenderer BossBodySr { get; private set; }
}
