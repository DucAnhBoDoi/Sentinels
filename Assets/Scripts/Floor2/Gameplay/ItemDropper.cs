using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG

public class ItemDropper : MonoBehaviour
{
    [Header("Danh sách các loại Năng lượng")]
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
            int randomIndex = Random.Range(0, energyOrbPrefabs.Length);
            
            // 1. Lưu vật phẩm vừa đẻ ra vào một biến
            GameObject droppedItem = Instantiate(energyOrbPrefabs[randomIndex], transform.position, Quaternion.identity);
            
            // 2. GỌI LỆNH SPAWN MẠNG ĐỂ CLIENT CŨNG THẤY
            NetworkObject netObj = droppedItem.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
            }
            
            Debug.Log("<color=cyan>Quái đã rơi ra vật phẩm!</color>");
        }
    }
}