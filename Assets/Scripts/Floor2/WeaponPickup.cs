using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponPickup : MonoBehaviour
{
    public string weaponName; // Tên vũ khí (ví dụ: "Shotgun", "Sword")
    public float pickupRange = 2.5f;
    
    [Header("Hiệu ứng lơ lửng")]
    public float amplitude = 0.2f;
    public float frequency = 2f;
    private Vector3 startPos;

    void Start() {
        startPos = transform.position;
    }

    void Update()
    {
        // Hiệu ứng lơ lửng cho dễ thấy
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Kiểm tra nhấn F để nhặt
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            CheckPickup();
        }
    }

    void CheckPickup()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance <= pickupRange)
        {
            Debug.Log("Đã nhặt vũ khí: " + weaponName);
            
            // Lệnh thay đổi vũ khí trên Player (Ví dụ)
            // player.GetComponent<PlayerCombat>().EquipWeapon(weaponName);

            Destroy(gameObject); // Biến mất sau khi nhặt
        }
    }
}