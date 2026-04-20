using UnityEngine;
using UnityEngine.InputSystem;

public class ShardCollector : MonoBehaviour
{
    public float floatAmplitude = 0.3f;
    public float floatFrequency = 2f;
    public float pickupRange = 2f;
    public LayerMask playerLayer;

    private Vector3 startPos;

    void Start() { startPos = transform.position; }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        if (Keyboard.current.fKey.wasPressedThisFrame) CheckPickup();
    }

    void CheckPickup()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayer);
        foreach (Collider2D col in colliders)
        {
            PlayerHP ph = col.GetComponentInParent<PlayerHP>();
            if (ph != null && !ph.IsDead)
            {

                // GỌI HÀM LEVEL COMPLETE CHO TẦNG 2
                if (Floor2Manager.Instance != null)
                {
                    Floor2Manager.Instance.LevelComplete();
                }

                Destroy(gameObject);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}