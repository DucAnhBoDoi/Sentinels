using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; 

public class EnergyOrb : MonoBehaviour
{
    public enum OrbType { Health, SpeedBoost, StopTime }
    
    [Header("Cấu hình loại vật phẩm")]
    public OrbType type; 
    public float speedBoostValue = 10f; 
    public float duration = 5f; // Thời gian tác dụng cho cả Speed và StopTime

    [Header("Cấu hình nhặt đồ")]
    public float pickupRange = 2.5f;   

    [Header("Hiệu ứng lơ lửng")]
    public float amplitude = 0.2f;
    public float frequency = 2f;
    
    private Vector3 startPos;
    private Transform player;
    private bool isCollected = false;

    void Start() {
        startPos = transform.position;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("EnergyOrb: Không tìm thấy đối tượng có Tag là 'Player'!");
    }

    void Update()
    {
        if (player == null || isCollected) return;

        // 1. Hiệu ứng lơ lửng cho vật phẩm trên mặt đất
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 2. Kiểm tra khoảng cách và nhấn F để nhặt
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= pickupRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Collect();
        }
    }

    void Collect()
    {
        isCollected = true;
        ApplyEffect(player.gameObject);
        
        // Ẩn vật phẩm nhưng không Destroy ngay để Coroutine chạy hết
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        if (type == OrbType.Health) Destroy(gameObject);
        else Destroy(gameObject, duration + 0.5f);
    }

    void ApplyEffect(GameObject playerObj)
    {
        switch (type)
        {
            case OrbType.Health:
                PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
                if (ph != null) ph.Heal(ph.maxHealth); 
                break;

            case OrbType.SpeedBoost:
                PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
                if (pm != null) StartCoroutine(SpeedBoostCoroutine(pm));
                break;

            case OrbType.StopTime:
                StartCoroutine(StopTimeCoroutine());
                break;
        }
    }

    // --- LOGIC NGƯNG ĐỘNG THỜI GIAN ---
    IEnumerator StopTimeCoroutine()
    {
        Debug.Log("<color=blue>ZA WARUDO! Ngưng đọng thời gian!</color>");

        // 1. Tìm tất cả quái vật (Phải có Tag là "Enemy")
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        // 2. Đóng băng tất cả quái đang có
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            // Tắt script di chuyển (Lấy tất cả script trừ Animator và Transform)
            MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
            foreach (var s in scripts) {
                if (s != this) s.enabled = false;
            }

            // Dừng vật lý (Dùng linearVelocity cho Unity 6)
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Dừng Animation
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.speed = 0;
        }

        // 3. Đợi trong khoảng thời gian duration (5 giây)
        yield return new WaitForSeconds(duration);

        // 4. Giải băng cho quái (Chỉ những con còn sống)
        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy == null) continue; // Bỏ qua nếu quái đã bị chém chết

            // Bật lại các script
            MonoBehaviour[] scripts = enemy.GetComponents<MonoBehaviour>();
            foreach (var s in scripts) s.enabled = true;

            // Cho Animation chạy lại
            Animator anim = enemy.GetComponent<Animator>();
            if (anim != null) anim.speed = 1;
        }

        Debug.Log("<color=white>Thời gian tiếp tục trôi...</color>");
    }

    IEnumerator SpeedBoostCoroutine(PlayerMovement pm)
    {
        float originalSpeed = pm.moveSpeed;
        pm.moveSpeed += speedBoostValue; 
        yield return new WaitForSeconds(duration); 
        pm.moveSpeed = originalSpeed; 
    }
}