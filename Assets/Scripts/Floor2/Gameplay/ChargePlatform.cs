using UnityEngine;

public class ChargePlatform : MonoBehaviour
{
    private LifeCore coreScript;
    public float detectionRange = 1.5f; 
    public LayerMask playerLayer; 
    private bool isPlayerNearby = false;

    // Lưu trữ Player đang đứng trên bệ này
    private PlayerHealth lastPlayerOnPlatform;

    void Start() {
        GameObject coreObj = GameObject.FindGameObjectWithTag("TheCore");
        if (coreObj != null) {
            coreScript = coreObj.GetComponent<LifeCore>();
        }
    }

    void Update() {
        if (coreScript == null) return;

        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);

        if (playerCollider != null && playerCollider.CompareTag("Player")) {
            if (!isPlayerNearby) {
                isPlayerNearby = true;
                coreScript.SetPlayerOnPlatform(true);
                
                // Đánh dấu Player này đang hiến máu
                lastPlayerOnPlatform = playerCollider.GetComponent<PlayerHealth>();
                if (lastPlayerOnPlatform != null) lastPlayerOnPlatform.isOnPlatform = true;
                
                Debug.Log("Player đang sạc tại: " + gameObject.name);
            }
        } else {
            if (isPlayerNearby) {
                isPlayerNearby = false;
                coreScript.SetPlayerOnPlatform(false);
                
                // Bỏ đánh dấu khi Player rời đi
                if (lastPlayerOnPlatform != null) {
                    lastPlayerOnPlatform.isOnPlatform = false;
                    lastPlayerOnPlatform = null;
                }
                
                Debug.Log("Player rời bệ: " + gameObject.name);
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green; 
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}