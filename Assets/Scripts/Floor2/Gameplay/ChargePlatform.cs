using UnityEngine;

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
        if (coreScript == null) return;

        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        PlayerHP currentPh = null;

        // TÌM SCRIPT TRÊN CẢ OBJECT CHA (Bỏ qua vụ Tag)
        if (playerCollider != null) {
            currentPh = playerCollider.GetComponentInParent<PlayerHP>();
        }

        if (currentPh != null && !currentPh.IsDead) {
            if (!isPlayerNearby) {
                isPlayerNearby = true;
                coreScript.SetPlayerOnPlatform(true);
                
                lastPlayerOnPlatform = currentPh;
                lastPlayerOnPlatform.isOnPlatform = true;
                
                Debug.Log("<color=yellow>Player đã dẫm lên bệ: </color>" + gameObject.name);
            }
        } else {
            if (isPlayerNearby) {
                isPlayerNearby = false;
                coreScript.SetPlayerOnPlatform(false);
                
                if (lastPlayerOnPlatform != null) {
                    lastPlayerOnPlatform.isOnPlatform = false;
                    lastPlayerOnPlatform = null;
                }
                Debug.Log("Player đã rời bệ: " + gameObject.name);
            }
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.green; 
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}