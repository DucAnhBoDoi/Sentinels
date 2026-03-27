using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [Header("Danh sách Súng/Kiếm Prefab")]
    public GameObject[] lootPrefabs; 

    [Range(0, 100)]
    public float dropChance = 100f; // Để 100 để chắc chắn rơi khi test

    // QUAN TRỌNG: Tên hàm phải TRÙNG KHỚP hoàn toàn với bên PlayerCombat
    public void DropRandomItem() 
    {
        if (lootPrefabs == null || lootPrefabs.Length == 0) 
        {
            Debug.LogWarning("Bạn chưa kéo Prefab vũ khí vào ô Loot Prefabs trên con Quái!");
            return;
        }

        float roll = Random.Range(0f, 100f);
        if (roll <= dropChance)
        {
            int index = Random.Range(0, lootPrefabs.Length);
            // Tạo vũ khí tại vị trí quái chết
            Instantiate(lootPrefabs[index], transform.position, Quaternion.identity);
            Debug.Log("<color=yellow>Quái đã rơi đồ!</color>");
        }
    }
}