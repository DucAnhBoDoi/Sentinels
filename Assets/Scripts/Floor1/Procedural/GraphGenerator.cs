using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG

// ĐỔI THÀNH NETWORK BEHAVIOUR
public class GraphGenerator : NetworkBehaviour
{
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

    // HÀM START ĐÃ BỊ THAY BẰNG HÀM MẠNG NÀY:
    public override void OnNetworkSpawn()
    {
        // CHỈ HOST MỚI ĐƯỢC PHÉP VẼ MAP VÀ ĐẺ MẠCH ĐIỆN
        if (IsServer)
        {
            GenerateWithRetries();
        }
    }

    void GenerateWithRetries()
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (TryGeneratePath())
            {
                Debug.Log($"<color=green>Sinh map Tầng 1 THÀNH CÔNG sau {attempt} lần thử lại!</color>");
                return;
            }
            else
            {
                ClearPath();
            }
        }
        Debug.LogError($"Không thể nhét đủ {numberOfNodes} Node vào map. Hãy giảm số Node hoặc giảm Wire Spacing xuống.");
    }

    void ClearPath()
    {
        foreach (GameObject obj in spawnedObjects) 
        {
            var netObj = obj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
            else Destroy(obj);
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
            // Bỏ transform ở đây đi để an toàn tuyệt đối trên mạng
            GameObject node = Instantiate(nodePrefab, currentPos, Quaternion.identity);
            
            // LÀM GIẤY KHAI SINH MẠNG CHO HỘP ĐIỆN
            node.GetComponent<NetworkObject>().Spawn(true);
            
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
                GameObject wire = Instantiate(wirePrefab, currentPos, Quaternion.Euler(0, 0, angle));
                
                // LÀM GIẤY KHAI SINH MẠNG CHO DÂY ĐIỆN
                wire.GetComponent<NetworkObject>().Spawn(true);

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