using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GraphGenerator : NetworkBehaviour
{
    public NetworkObject SpawnLocation; 
    [Header("Prefabs từ thư mục Floor1")]
    public GameObject nodePrefab; 
    public GameObject wirePrefab; 
    
    [Header("Cấu hình sơ đồ mạch điện")]
    public int numberOfNodes = 15;       
    public int minWireLength = 4;       
    public int maxWireLength = 15;      
    public float tileSize = 1f;         

    [Header("Tránh Tường (Cấu hình Layer)")]
    public LayerMask obstacleLayer; 

    [Header("Cấu hình Thuật toán (AI)")]
    public int maxRetries = 100; 
    [Range(0f, 1f)]
    public float turnProbability = 0.8f; 
    public float wireSpacing = 2f; 

    private List<Vector2> visitedPositions = new List<Vector2>();
    private List<GameObject> spawnedObjects = new List<GameObject>(); 

    public override void OnNetworkSpawn()
    {
       if (IsServer)
        {
            Invoke(nameof(GenerateWithRetries), 0.5f);
        }
    }

    void GenerateWithRetries()
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (TryGeneratePath())
            {
                Debug.Log($"<color=green>Sinh map Tầng 1 THÀNH CÔNG sau {attempt} lần thử lại!</color>");
                
                // SỬA LỖI TRIỆT ĐỂ TẠI ĐÂY: 
                // Khi thuật toán đã vẽ map thành công 100%, ta mới bắt đầu báo cho Client Spawn mạng
                foreach (GameObject obj in spawnedObjects)
                {
                    var netObj = obj.GetComponent<NetworkObject>();
                    if (!netObj.IsSpawned)
                    {
                        netObj.Spawn(true);
                        netObj.TrySetParent(SpawnLocation);
                    }
                }
                return;
            }
            else
            {
                // Nếu vẽ lỗi, ta xóa nháp đi (Lúc này vật thể chưa lên mạng nên không sợ sập mạng Client)
                ClearPath();
            }
        }
        Debug.LogError($"Không thể nhét đủ {numberOfNodes} Node vào map. Hãy giảm số Node hoặc giảm Wire Spacing xuống.");
    }

    void ClearPath()
    {
        foreach (GameObject obj in spawnedObjects) 
        {
            // Vì chưa gọi Spawn lên mạng, ta chỉ cần Destroy bình thường
            Destroy(obj);
        }
        spawnedObjects.Clear();
        visitedPositions.Clear();
    }

    bool TryGeneratePath()
    {
        Vector2 currentPos = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
        Vector2 lastDirection = Vector2.zero;

        for (int i = 0; i < numberOfNodes; i++)
        {
            // 1. TẠO NHÁP: Khởi tạo thẳng ở currentPos, KHÔNG GỌI Spawn() ở đây
            GameObject node = Instantiate(nodePrefab, currentPos, Quaternion.identity);
            
            spawnedObjects.Add(node);
            visitedPositions.Add(currentPos);

            if (i == numberOfNodes - 1) return true; 

            int currentWireLength = Random.Range(minWireLength, maxWireLength + 1);
            Vector2 direction = Vector2.zero;

            while (currentWireLength >= minWireLength)
            {
                direction = GetSmartDirection(currentPos, currentWireLength, lastDirection);
                if (direction != Vector2.zero) break; 
                currentWireLength--; 
            }
            
            if (direction == Vector2.zero) return false; 

            lastDirection = direction; 

            for (int j = 0; j < currentWireLength; j++)
            {
                currentPos += direction * tileSize;
                float angle = (direction.y != 0) ? 90f : 0f;

                // 2. TẠO NHÁP: Khởi tạo thẳng ở currentPos, KHÔNG GỌI Spawn() ở đây
                GameObject wire = Instantiate(wirePrefab, currentPos, Quaternion.Euler(0, 0, angle));
                
                spawnedObjects.Add(wire);
                visitedPositions.Add(currentPos);
            }
            currentPos += direction * tileSize;
        }
        return true;
    }

    Vector2 GetSmartDirection(Vector2 current, int targetLength, Vector2 lastDir)
    {
        List<Vector2> validDirections = new List<Vector2> { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        if (lastDir != Vector2.zero) validDirections.Remove(-lastDir);

        for (int i = 0; i < validDirections.Count; i++)
        {
            Vector2 temp = validDirections[i];
            int randomIndex = Random.Range(i, validDirections.Count);
            validDirections[i] = validDirections[randomIndex];
            validDirections[randomIndex] = temp;
        }

        if (lastDir != Vector2.zero && Random.value < turnProbability)
        {
            if (validDirections.Contains(lastDir))
            {
                validDirections.Remove(lastDir);
                validDirections.Add(lastDir); 
            }
        }

        foreach (Vector2 dir in validDirections)
        {
            bool isPathClear = true;
            for (int step = 1; step <= targetLength + 1; step++)
            {
                Vector2 checkPos = current + (dir * tileSize * step);
                
                Collider2D hitCollider = Physics2D.OverlapCircle(checkPos, 0.3f, obstacleLayer);
                if (hitCollider != null) { isPathClear = false; break; }

                foreach (Vector2 oldPos in visitedPositions)
                {
                    if (Vector2.Distance(checkPos, oldPos) < wireSpacing * tileSize)
                    {
                        if (Vector2.Distance(current, oldPos) >= wireSpacing * tileSize)
                        {
                            isPathClear = false;
                            break;
                        }
                    }
                }
                if (!isPathClear) break;
            }
            if (isPathClear) return dir;
        }
        return Vector2.zero;
    }
}