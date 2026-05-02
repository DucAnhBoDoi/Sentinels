using UnityEngine;
using System.Collections;
using Unity.Netcode; 

public class PlayerCombat : NetworkBehaviour
{
    [Header("Cấu hình vùng đánh")]
    public Transform attackPoint;
    public float attackRange = 1.5f;
    public LayerMask enemyLayers;

    [Header("Cấu hình phím đánh")]
    public KeyCode attackKey = KeyCode.Mouse0;

    [Header("Hồi máu khi giết quái")]
    public float healthRegenPerKill = 6f;

    [Header("Cấu hình Delay")]
    public float attackDelay = 0.15f;

    // --- THÊM CẤU HÌNH ÂM THANH Ở ĐÂY ---
    [Header("Cấu hình Âm thanh")]
    public AudioSource audioSource;
    public AudioClip swingSound;
    public AudioClip hitSound;

    private Animator animator;
    private Unity.Netcode.Components.NetworkAnimator netAnim; 
    private bool isHitStopping = false;

    void Start() 
    { 
        animator = GetComponent<Animator>(); 
        netAnim = GetComponent<Unity.Netcode.Components.NetworkAnimator>();

        // Tự động tìm AudioSource trên người nhân vật
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!IsOwner) return;

        if (!QuestPopupManager.isGameStarted) return;
        if (Input.GetKeyDown(attackKey)) StartCoroutine(AttackWithDelay());
    }

    IEnumerator AttackWithDelay()
    {
        // 1. DÙNG NETWORK ANIMATOR ĐỂ BUNG HOẠT ẢNH TRÊN TOÀN MẠNG
        if (netAnim != null) netAnim.SetTrigger("isAttacking");
        else if (animator != null) animator.SetTrigger("isAttacking");

        // --- GỌI ÂM THANH VUNG KIẾM LÊN MẠNG ---
        PlaySwingSoundServerRpc();

        yield return new WaitForSeconds(attackDelay);
        PerformDamage();
    }

    // --- LOA PHƯỜNG: BÁO CHO MỌI NGƯỜI NGHE TIẾNG VUNG KIẾM ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlaySwingSoundServerRpc()
    {
        PlaySwingSoundClientRpc();
    }

    [ClientRpc]
    private void PlaySwingSoundClientRpc()
    {
        if (audioSource != null && swingSound != null)
        {
            audioSource.PlayOneShot(swingSound);
        }
    }

    void PerformDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        bool hasHitSomething = false; // Biến cờ hiệu kiểm tra chém trúng

        foreach (Collider2D enemy in hitEnemies)
        {
            IDamagable damageable = enemy.GetComponentInParent<IDamagable>();

            if (damageable != null)
            {
                damageable.TakeDamage();
                hasHitSomething = true; // Bật cờ hiệu

                // 2. GỌI LỆNH MẠNG ĐỂ CẢ HOST VÀ CLIENT CÙNG CHẠY HITSTOP
                ApplyHitStopClientRpc();

                PlayerHP ph = GetComponent<PlayerHP>();
                if (ph != null) ph.TakeDamage(-(int)healthRegenPerKill);
            }
        }

        // --- BÁO CHO MỌI NGƯỜI NGHE TIẾNG TRÚNG ĐÍCH ---
        if (hasHitSomething)
        {
            PlayHitSoundServerRpc();
        }
    }

    // --- LOA PHƯỜNG: BÁO CHO MỌI NGƯỜI NGHE TIẾNG CHÉM TRÚNG ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PlayHitSoundServerRpc()
    {
        PlayHitSoundClientRpc();
    }

    [ClientRpc]
    private void PlayHitSoundClientRpc()
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }

    // HÀM MẠNG: BẮT TẤT CẢ CÁC MÁY CÙNG KHỰNG HÌNH LẠI
    [ClientRpc]
    void ApplyHitStopClientRpc()
    {
        StartCoroutine(HitStop(0.05f));
    }

    IEnumerator HitStop(float duration)
    {
        if (isHitStopping) yield break;
        isHitStopping = true;

        if (animator != null) animator.speed = 0.05f;
        yield return new WaitForSeconds(duration);
        if (animator != null) animator.speed = 1f;

        isHitStopping = false;
    }

    void OnDrawGizmos()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}