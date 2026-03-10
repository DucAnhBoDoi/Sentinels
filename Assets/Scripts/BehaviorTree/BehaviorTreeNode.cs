using UnityEngine;

public abstract class BehaviorTreeNode : MonoBehaviour
{
    public enum TaskStatus
    {
        Inactive,
        Running,
        Success,
        Failure,
    }

    public TaskStatus BehaviorStatus { get; protected set; }

    public virtual void BehaviorStart()
    {
        BehaviorStatus = TaskStatus.Running;
    }

    public virtual void BehaviorEnd()
    {
        BehaviorStatus = TaskStatus.Inactive;
    }
}
