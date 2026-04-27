using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class EnergyOrb : NetworkBehaviour
{
    public enum OrbType { Health, SpeedBoost, StopTime }
    
    [Header("Cấu hình loại vật phẩm")]
    public OrbType type; 
    public float speedBoostValue = 10f; 
    public float duration = 5f; 
    public float pickupRange = 2.5f;   
    public LayerMask playerLayer;

    [Header("Hiệu ứng lơ lửng")]
    public float amplitude = 0.2f;
    public float frequency = 2f;
    
    private Vector3 startPos;
    private bool isCollected = false;

    void Start() { startPos = transform.position; }

    void Update() {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        if (!IsSpawned || isCollected) return;

        if (Keyboard.current.fKey.wasPressedThisFrame) {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayer);
            foreach (Collider2D col in colliders) {
                NetworkObject playerNetObj = col.GetComponentInParent<NetworkObject>();
                
                if (playerNetObj != null && playerNetObj.IsOwner) {
                    PlayerHP ph = col.GetComponentInParent<PlayerHP>();
                    if (ph != null && !ph.IsDead) {
                        ClaimOrbServerRpc(playerNetObj.NetworkObjectId);
                        break; 
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    void ClaimOrbServerRpc(ulong playerNetworkObjectId) {
        if (isCollected) return;
        isCollected = true;

        ApplyEffectClientRpc(playerNetworkObjectId);

        if (type == OrbType.StopTime) {
            StartCoroutine(StopTimeCoroutineServer()); 
            HideOrbClientRpc(); 
        } else {
            NetworkObject.Despawn(true); 
        }
    }

    [ClientRpc]
    void HideOrbClientRpc() {
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }

    [ClientRpc]
    void ApplyEffectClientRpc(ulong playerNetworkObjectId) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerObj)) return;
        
        GameObject collector = playerObj.gameObject;

        if (type == OrbType.Health) {
            PlayerHP ph = collector.GetComponent<PlayerHP>();
            if (ph != null && ph.IsOwner) ph.Heal(50f); 
        }
        else if (type == OrbType.SpeedBoost) {
            PlayerMovement pm = collector.GetComponent<PlayerMovement>();
            if (pm != null && pm.IsOwner) {
                SpeedBoostTracker tracker = collector.GetComponent<SpeedBoostTracker>();
                if (tracker == null) tracker = collector.AddComponent<SpeedBoostTracker>();
                tracker.ApplyBoost(pm, speedBoostValue, duration);
            }
        }
    }

    // =============================================================
    // HÀM MẠNG: BẢO TẤT CẢ CLIENT ĐÓNG BĂNG/GIẢI ĐÔNG ANIMATION QUÁI
    // =============================================================
    [ClientRpc]
    void SetEnemyAnimationSpeedClientRpc(bool isFrozen) {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) {
            if (enemy == null) continue;
            Animator anim = enemy.GetComponent<Animator>();
            // Nếu đóng băng thì speed = 0, giải đông thì speed = 1
            if (anim != null) anim.speed = isFrozen ? 0f : 1f; 
        }
    }

    IEnumerator StopTimeCoroutineServer() {
        // BƯỚC 1: Xử lý logic khóa quái trên Server
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) {
            if (enemy == null) continue;
            
            BoidEnemy boid = enemy.GetComponent<BoidEnemy>();
            if (boid != null) boid.enabled = false;

            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        
        // BƯỚC 2: Gọi lệnh báo cho tất cả Client (bao gồm cả Host) đóng băng hình ảnh
        SetEnemyAnimationSpeedClientRpc(true);
        
        yield return new WaitForSeconds(duration);
        
        // BƯỚC 3: Hết thời gian, mở khóa logic trên Server
        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in activeEnemies) {
            if (enemy == null) continue;
            
            BoidEnemy boid = enemy.GetComponent<BoidEnemy>();
            if (boid != null) boid.enabled = true;
        }

        // BƯỚC 4: Báo cho Client mở khóa hình ảnh
        SetEnemyAnimationSpeedClientRpc(false);

        NetworkObject.Despawn(true); 
    }
}

// CLASS ĐỒNG HỒ THEO DÕI TỐC ĐỘ
public class SpeedBoostTracker : MonoBehaviour
{
    private PlayerMovement pm;
    private float currentBoost = 0f;
    private float timeLeft = 0f;

    public void ApplyBoost(PlayerMovement playerMovement, float boostValue, float duration)
    {
        pm = playerMovement;
        if (currentBoost == 0f)
        {
            currentBoost = boostValue;
            pm.moveSpeed += currentBoost;
        }
        timeLeft = duration;
    }

    void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0)
            {
                if (pm != null) pm.moveSpeed -= currentBoost; 
                Destroy(this); 
            }
        }
    }
}