using UnityEngine;

public class ItemDropper : MonoBehaviour
{
    [Header("Danh sách các loại Năng lượng")]
    // Kéo cả 2 Prefab (fulloblood và runfast) vào danh sách này
    public GameObject[] energyOrbPrefabs; 

    [Range(0, 100)]
    public float dropChance = 50f; 

    public void DropRandomItem() 
    {
        if (energyOrbPrefabs == null || energyOrbPrefabs.Length == 0) 
        {
            Debug.LogWarning("Chưa kéo Prefab vào danh sách trên con Quái!");
            return;
        }

        float roll = Random.Range(0f, 100f);
        
        if (roll <= dropChance)
        {
            // Chọn ngẫu nhiên 1 trong các Prefab có trong danh sách
            int randomIndex = Random.Range(0, energyOrbPrefabs.Length);
            
            Instantiate(energyOrbPrefabs[randomIndex], transform.position, Quaternion.identity);
            
            Debug.Log("<color=cyan>Quái đã rơi ra vật phẩm!</color>");
        }
    }
}