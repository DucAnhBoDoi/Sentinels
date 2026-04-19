using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; 

public class EnergyOrb : MonoBehaviour
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
        if (isCollected) return;

        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, pickupRange, playerLayer);
        foreach (Collider2D col in colliders) {
            
            PlayerHP ph = col.GetComponentInParent<PlayerHP>();
            
            if (ph != null && !ph.IsDead && Keyboard.current.fKey.wasPressedThisFrame) {
                Collect(ph.gameObject);
                break; 
            }
        }
    }

    void Collect(GameObject collector) {
        isCollected = true;
        ApplyEffect(collector);
        
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        // Chỉ có StopTime mới cần giữ object sống để chạy Coroutine giải băng quái
        if (type == OrbType.StopTime) Destroy(gameObject, duration + 0.5f);
        else Destroy(gameObject); // Máu và Tốc độ ăn xong là xóa luôn cho nhẹ máy
    }

    void ApplyEffect(GameObject playerObj) {
        switch (type) {
            case OrbType.Health:
                PlayerHP ph = playerObj.GetComponent<PlayerHP>();
                if (ph != null) ph.Heal(50f); 
                break;

            case OrbType.SpeedBoost:
                PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
                if (pm != null) 
                {
                    // Tìm xem Player đã có cái đồng hồ đếm giờ chưa, chưa có thì gắn vào
                    SpeedBoostTracker tracker = playerObj.GetComponent<SpeedBoostTracker>();
                    if (tracker == null) tracker = playerObj.AddComponent<SpeedBoostTracker>();
                    
                    // Kích hoạt hoặc Reset thời gian
                    tracker.ApplyBoost(pm, speedBoostValue, duration);
                }
                break;

            case OrbType.StopTime:
                StartCoroutine(StopTimeCoroutine());
                break;
        }
    }

    IEnumerator StopTimeCoroutine() {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) {
            if (enemy == null) continue;
            MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
            foreach (var s in scripts) { if (s != this) s.enabled = false; }
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.speed = 0;
        }
        yield return new WaitForSeconds(duration);
        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in activeEnemies) {
            if (enemy == null) continue;
            MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
            foreach (var s in scripts) s.enabled = true;
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.speed = 1;
        }
    }
}

// =====================================================================
// CLASS MỚI: ĐỒNG HỒ THEO DÕI TỐC ĐỘ (Gắn tạm lên Player khi ăn buff)
// =====================================================================
public class SpeedBoostTracker : MonoBehaviour
{
    private PlayerMovement pm;
    private float currentBoost = 0f;
    private float timeLeft = 0f;

    public void ApplyBoost(PlayerMovement playerMovement, float boostValue, float duration)
    {
        pm = playerMovement;
        
        // Nếu ĐANG CHƯA CÓ tốc độ cộng thêm thì mới cộng (Chống cộng dồn)
        if (currentBoost == 0f)
        {
            currentBoost = boostValue;
            pm.moveSpeed += currentBoost;
        }
        
        // Dù mới ăn cục đầu hay ăn cục thứ 10, cứ reset đồng hồ về 5 giây
        timeLeft = duration;
    }

    void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            
            // Khi đếm ngược về 0 (Hết tác dụng)
            if (timeLeft <= 0)
            {
                if (pm != null) pm.moveSpeed -= currentBoost; // Trừ đi đúng lượng đã cộng
                Destroy(this); // Xóa luôn cái đồng hồ này khỏi Player cho sạch sẽ
            }
        }
    }
}