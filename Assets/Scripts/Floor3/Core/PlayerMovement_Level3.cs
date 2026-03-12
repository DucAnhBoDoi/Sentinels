// ══════════════════════════════════════════════════════════════
// FILE: Assets/Scripts/Floor3/PlayerMovement_Level3.cs
// PURPOSE: Movement script dedicated for Floor3
// NOTE:
//   - Independent from Floor1 systems
//   - No QuestPopupManager dependency
//   - No Robot combat / repair mechanics
//   - Designed for escort + quiz gameplay
// ══════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement_Level3 : MonoBehaviour
{
    public enum PlayerType { PlayerA, PlayerB }
    public PlayerType playerType;

    public float moveSpeed = 5f;

    public Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        movement = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Player A (WASD)
        if (playerType == PlayerType.PlayerA)
        {
            if (keyboard.wKey.isPressed)
                movement.y = 1f;
            else if (keyboard.sKey.isPressed)
                movement.y = -1f;

            if (keyboard.dKey.isPressed)
                movement.x = 1f;
            else if (keyboard.aKey.isPressed)
                movement.x = -1f;
        }

        // Player B (Arrow Keys)
        else if (playerType == PlayerType.PlayerB)
        {
            if (keyboard.upArrowKey.isPressed)
                movement.y = 1f;
            else if (keyboard.downArrowKey.isPressed)
                movement.y = -1f;

            if (keyboard.rightArrowKey.isPressed)
                movement.x = 1f;
            else if (keyboard.leftArrowKey.isPressed)
                movement.x = -1f;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position +
            movement.normalized * moveSpeed * Time.fixedDeltaTime
        );
    }
}