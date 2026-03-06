using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMove))]
public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Move,
    }

    [field: SerializeField]
    public PlayerStatsSO Stats { get; private set; }

    public Rigidbody2D Rb { get; private set; }

    [HideInInspector]
    public PlayerState State;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
    }
}
