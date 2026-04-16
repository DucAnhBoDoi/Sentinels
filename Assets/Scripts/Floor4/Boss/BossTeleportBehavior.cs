using UnityEngine;

[RequireComponent(typeof(BossBehaviorManager))]
public class BossTeleportBehavior : BehaviorTreeNode
{
    [SerializeField]
    private Transform _teleportPoints;

    private BossBehaviorManager _boss;

    private void Awake()
    {
        _boss = GetComponent<BossBehaviorManager>();
    }

    public override void BehaviorStart()
    {
        float averageDistance = float.MinValue;
        Vector2 location = Vector2.zero;
        foreach (Transform point in _teleportPoints)
        {
            float sum = 0;
            foreach (GameObject player in _boss.Players)
            {
                sum += Vector2.Distance(player.transform.position, point.position);
            }
            float avg = sum / _boss.Players.Length;
            if (avg > averageDistance)
            {
                averageDistance = avg;
                location = point.position;
            }
        }
        transform.position = location;
        BehaviorStatus = TaskStatus.Success;
    }
}
