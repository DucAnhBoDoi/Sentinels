using UnityEngine;

[CreateAssetMenu(fileName = "LANBroadcastConfigSO", menuName = "Scriptable Objects/LANBroadcastConfigSO")]
public class LANBroadcastConfigSO : ScriptableObject
{
    [field: SerializeField]
    public int BroadcastPort { get; private set; }
}
