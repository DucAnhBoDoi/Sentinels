using UnityEngine;
using Unity.Netcode;

public class ChargePlatform : MonoBehaviour
{
    private LifeCore coreScript;
    public float detectionRange = 1.5f; 
    public LayerMask playerLayer; 
    private bool isPlayerNearby = false;
    private PlayerHP lastPlayerOnPlatform;

    void Start() {
        GameObject coreObj = GameObject.FindGameObjectWithTag("TheCore");
        if (coreObj != null) coreScript = coreObj.GetComponent<LifeCore>();
    }

    void Update() {
        // CHỈ SERVER MỚI QUÉT XEM CÓ AI DẪM LÊN BỆ KHÔNG
        if (coreScript == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        PlayerHP currentPh = null;

        if (playerCollider != null) {
            currentPh = playerCollider.GetComponentInParent<PlayerHP>();
        }

        if (currentPh != null && !currentPh.IsDead) {
            if (!isPlayerNearby) {
                isPlayerNearby = true;
                coreScript.SetPlayerOnPlatform(true);
                lastPlayerOnPlatform = currentPh;
                lastPlayerOnPlatform.isOnPlatform = true;
            }
        } else {
            if (isPlayerNearby) {
                isPlayerNearby = false;
                coreScript.SetPlayerOnPlatform(false);
                if (lastPlayerOnPlatform != null) {
                    lastPlayerOnPlatform.isOnPlatform = false;
                    lastPlayerOnPlatform = null;
                }
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green; 
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}