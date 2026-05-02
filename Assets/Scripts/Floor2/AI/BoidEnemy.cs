using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode; // BẮT BUỘC CÓ

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(Animator))]
public class BoidEnemy : NetworkBehaviour, IDamagable 
{
    [Header("Boid Settings")]
    public float speed = 3.0f;
    public float neighborRadius = 3.5f;
    public float attackDamage = 5f;

    [Range(0, 10)] public float separationWeight = 8.0f; 
    [Range(0, 5)] public float cohesionWeight = 0.5f;    
    [Range(0, 5)] public float alignmentWeight = 1.0f;
    public float targetWeight = 2.0f;

    [Header("Anti-Clump")]
    public float separationDistance = 1.3f;     
    public float extraSeparationForce = 6.0f;   

    [Header("Cấu hình sinh tồn")]
    public int maxHealth = 3;
    // BIẾN MẠNG QUẢN LÝ MÁU
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool isDead = false;

    // --- THÊM: CẤU HÌNH HIỆU ỨNG VÀ KNOCKBACK TẠI ĐÂY ---
    [Header("Hiệu ứng & Knockback")]
    public ParticleSystem hitParticles;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    private bool isKnockedBack = false;

    [Header("Cấu hình tấn công")]
    public float attackDistance = 1.2f; 
    public Vector2 attackOffset;
    public Vector2 damageRangeScale = new Vector2(1.5f, 1.5f); 
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Tham chiếu")]
    private Transform target;
    private Animator anim;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Unity.Netcode.Components.NetworkAnimator netAnim; // THÊM BIẾN MẠNG ANIMATOR

    private Vector2 currentVelocity;
    private static List<BoidEnemy> allBoids = new List<BoidEnemy>();

    // BIẾN LƯU MÀU GỐC ĐỂ KHÔNG BỊ MẤT MÀU
    private Color baseColor = Color.white;

    // BIẾN MẠNG ĐỂ LẬT MẶT QUÁI CHO CLIENT THẤY
    private NetworkVariable<bool> isFlipped = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        netAnim = GetComponent<Unity.Netcode.Components.NetworkAnimator>();

        // LƯU LẠI MÀU GỐC LÚC MỚI ĐẺ RA
        if (sr != null) baseColor = sr.color;

        GameObject core = GameObject.FindGameObjectWithTag("TheCore");
        if (core != null) target = core.transform;

        currentVelocity = Random.insideUnitCircle.normalized * speed;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentHealth.Value = maxHealth;

        currentHealth.OnValueChanged += OnHealthChanged;
        isFlipped.OnValueChanged += OnFlipChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isFlipped.OnValueChanged -= OnFlipChanged;
    }

    private void OnHealthChanged(int prev, int current)
    {
        if (current <= 0 && !isDead)
        {
            Die();
        }
        else if (current < prev)
        {
            if (netAnim) netAnim.SetTrigger("isHurt");
            else if (anim) anim.SetTrigger("isHurt");

            // --- CHẠY HIỆU ỨNG CHỚP ĐỎ VÀ HẠT NỔ ---
            StartCoroutine(FlashRedRoutine());
            if (hitParticles != null) hitParticles.Play();
        }
    }

    // --- COROUTINE CHỚP ĐỎ (ĐÃ SỬA ĐỂ TRẢ VỀ BASE COLOR) ---
    private IEnumerator FlashRedRoutine()
    {
        if (sr == null) yield break;
        
        sr.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        sr.color = baseColor; // Trả về màu gốc (Vàng, Đỏ hoặc Trắng)
    }

    private void OnFlipChanged(bool prev, bool current)
    {
        if (sr) sr.flipX = current;
    }

    public void TakeDamage()
    {
        if (isDead) return;
        TakeDamageServerRpc(); // Yêu cầu Server trừ máu
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TakeDamageServerRpc()
    {
        if (isDead) return;
        currentHealth.Value--;

        // --- GỌI XỬ LÝ KNOCKBACK TRÊN SERVER ---
        StartCoroutine(KnockbackRoutine());
    }

    // --- COROUTINE ĐẨY LÙI ---
    private IEnumerator KnockbackRoutine()
    {
        isKnockedBack = true;

        // Boid Enemy đẩy ngược lại hướng The Core (hoặc lùi lại sau lưng nếu không có core)
        Vector2 knockbackDir = Vector2.zero;
        if (target != null)
        {
            knockbackDir = (transform.position - target.position).normalized;
        }
        else
        {
            float facingDir = sr.flipX ? 1f : -1f;
            knockbackDir = new Vector2(facingDir, 0);
        }

        // Tạm thời đè lên vận tốc hiện tại để nó văng ra
        currentVelocity = knockbackDir * knockbackForce;

        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        
        if (netAnim) netAnim.SetTrigger("isDead");
        else if (anim) anim.SetTrigger("isDead");

        GetComponent<Collider2D>().enabled = false;

        // CHỈ SERVER MỚI ĐƯỢC RỚT ĐỒ VÀ XÓA MẠNG
        if (IsServer)
        {
            ItemDropper dropper = GetComponent<ItemDropper>();
            if (dropper != null) dropper.DropRandomItem();
            
            // Chờ 1.2s rồi xóa
            Invoke(nameof(DespawnEnemy), 1.2f);
        }
    }

    void DespawnEnemy()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    void Update()
    {
        if (isDead || target == null) return;

        // CHỈ SERVER MỚI TÍNH TOÁN AI DI CHUYỂN
        if (!IsServer) return;

        // --- NẾU ĐANG BỊ VĂNG THÌ KHÔNG TÍNH TOÁN FLOCKING ---
        if (isKnockedBack) return;

        float distToTarget = Vector2.Distance(transform.position, target.position);
        Collider2D targetCol = target.GetComponent<Collider2D>();
        Vector2 targetCenter = target.position;

        if (targetCol != null)
        {
            targetCenter = targetCol.bounds.center;
            Vector2 closestPoint = targetCol.ClosestPoint(transform.position);
            distToTarget = Vector2.Distance(transform.position, closestPoint);
        }

        if (distToTarget <= attackDistance)
        {
            Vector2 escapeDir = CalculatePureSeparation();
            currentVelocity = Vector2.Lerp(currentVelocity, escapeDir * speed * 0.5f, Time.deltaTime * 5f);

            if (netAnim) netAnim.Animator.SetBool("isRunning", false);
            
            // Server tự động cập nhật hướng nhìn cho các máy khác
            isFlipped.Value = (targetCenter.x > transform.position.x);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                if (netAnim) netAnim.SetTrigger("isAttacking");
                lastAttackTime = Time.time;
            }
        }
        else 
        { 
            HandleFlocking(targetCenter); 
        }
    }

    Vector2 CalculatePureSeparation()
    {
        Vector2 sep = Vector2.zero;
        foreach (BoidEnemy boid in allBoids)
        {
            if (boid == this || boid == null) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);
            if (dist < separationDistance)
            {
                sep += ((Vector2)transform.position - (Vector2)boid.transform.position).normalized / dist;
            }
        }
        return sep.normalized;
    }

    void HandleFlocking(Vector2 targetCenter)
    {
        if (netAnim) netAnim.Animator.SetBool("isRunning", true);

        Vector2 separation = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        int neighborCount = 0;

        foreach (BoidEnemy boid in allBoids)
        {
            if (boid == this || boid == null) continue;
            float dist = Vector2.Distance(transform.position, boid.transform.position);

            if (dist < neighborRadius)
            {
                Vector2 diff = ((Vector2)transform.position - (Vector2)boid.transform.position).normalized;
                float force = (dist < separationDistance) ? extraSeparationForce : 1.0f;
                separation += diff * (force / Mathf.Max(dist, 0.1f));

                cohesion += (Vector2)boid.transform.position;
                alignment += boid.currentVelocity;
                neighborCount++;
            }
        }

        Vector2 targetDir = (targetCenter - (Vector2)transform.position).normalized;
        Vector2 flockingDir = Vector2.zero;
        
        if (neighborCount > 0)
        {
            cohesion = ((cohesion / neighborCount) - (Vector2)transform.position).normalized;
            alignment = (alignment / neighborCount).normalized;
            flockingDir = (separation * separationWeight) + (cohesion * cohesionWeight) + (alignment * alignmentWeight);
        }

        Vector2 combinedDir = flockingDir + targetDir * targetWeight;
        combinedDir += Random.insideUnitCircle * 0.2f; 

        currentVelocity = Vector2.Lerp(currentVelocity, combinedDir.normalized * speed, Time.deltaTime * 4f);
        
        // Server cập nhật hướng nhìn lật mặt quái
        if (currentVelocity.magnitude > 0.1f) isFlipped.Value = currentVelocity.x > 0;
    }

    void FixedUpdate()
    {
        if (!isDead && IsServer) rb.linearVelocity = currentVelocity;
    }

    public void ExecuteBoidHit()
    {
        // Chỉ Server mới được tính sát thương đập vào Lõi
        if (target == null || isDead || !IsServer) return;

        float lookDir = sr.flipX ? 1f : -1f;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * lookDir, attackOffset.y);
        Collider2D[] hitObjects = Physics2D.OverlapBoxAll(attackCenter, damageRangeScale, 0f);

        foreach (Collider2D col in hitObjects)
        {
            if (col.CompareTag("TheCore"))
            {
                LifeCore coreScript = col.GetComponent<LifeCore>();
                if (coreScript != null) coreScript.TakeDirectDamage(attackDamage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        float lookDir = (sr != null && sr.flipX) ? 1f : -1f;
        Vector2 attackCenter = (Vector2)transform.position + new Vector2(attackOffset.x * lookDir, attackOffset.y);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackCenter, damageRangeScale);
    }

    void OnEnable() { if(!allBoids.Contains(this)) allBoids.Add(this); }
    void OnDisable() { allBoids.Remove(this); }

    // --- ĐÃ ĐỔI CẤU TRÚC HÀM SETCOLOR ĐỂ KHÔNG BỊ LỖI QUÊN MÀU ---
    public void SetColor(Color newColor)
    {
        ApplyColor(newColor);
        SetColorClientRpc(newColor);
    }

    [ClientRpc]
    public void SetColorClientRpc(Color newColor)
    {
        if (!IsServer) 
        {
            ApplyColor(newColor);
        }
    }

    private void ApplyColor(Color newColor)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) 
        {
            sr.color = newColor;
            baseColor = newColor; // Cập nhật màu gốc vĩnh viễn
        }
    }
}