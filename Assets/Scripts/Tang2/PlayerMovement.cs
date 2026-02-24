using UnityEngine;

public class PlayerMovement : MonoBehaviour 
{
    public float speed = 5f;
    public bool isPlayer1 = true;

    void Update() 
    {
        float moveX = 0;
        float moveY = 0;

        if (isPlayer1) 
        {
            // Player 1: Chỉ dùng WASD
            if (Input.GetKey(KeyCode.W)) moveY = 1;
            if (Input.GetKey(KeyCode.S)) moveY = -1;
            if (Input.GetKey(KeyCode.A)) moveX = -1;
            if (Input.GetKey(KeyCode.D)) moveX = 1;
        } 
        else 
        {
            // Player 2: Chỉ dùng các phím Mũi tên (Arrows)
            if (Input.GetKey(KeyCode.UpArrow)) moveY = 1;
            if (Input.GetKey(KeyCode.DownArrow)) moveY = -1;
            if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1;
            if (Input.GetKey(KeyCode.RightArrow)) moveX = 1;
        }

        // Di chuyển nhân vật
        Vector2 movement = new Vector2(moveX, moveY).normalized;
        transform.Translate(movement * speed * Time.deltaTime);
    }
}