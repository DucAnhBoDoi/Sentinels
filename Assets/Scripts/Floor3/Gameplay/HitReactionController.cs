// ============================================================
// FILE: Assets/Scripts/Shared/HitReactionController.cs
// ============================================================
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HitReactionController : MonoBehaviour
{
    [Header("HP (bỏ qua nếu enemy có HP riêng)")]
    public bool manageHp = true;
    public float maxHp = 3f;

    [Header("Flash trắng")]
    public float flashDuration = 0.12f;
    public float flashFadeOutDuration = 0.18f;

    [Header("Knockback")]
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.15f;

    [Header("Hit Shake")]
    public bool useShake = true;
    public float shakeMagnitude = 0.08f;
    public float shakeDuration = 0.2f;

    // ── Private ──────────────────────────────────────────────
    private float _currentHp;
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private bool _isDead = false;

    // Visual root — child object chứa sprite + flash layer
    // Rigidbody2D nằm ở parent, chỉ visual root mới rung
    private Transform _visualRoot;
    private SpriteRenderer _flashLayer;

    public bool IsBeingKnockedBack { get; private set; } = false;

    // ── Awake ─────────────────────────────────────────────────
    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _currentHp = maxHp;

        SetupVisualRoot();
        SetupFlashLayer();
    }

    // ── Setup: tạo child object làm visual root ───────────────
    private void SetupVisualRoot()
    {
        // Tạo child object — chỉ chứa visual, không có physics
        GameObject visualObj = new GameObject("_VisualRoot");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localScale = Vector3.one;
        _visualRoot = visualObj.transform;

        // Chuyển SpriteRenderer gốc sang visual root
        // Thực ra SpriteRenderer vẫn nằm ở parent object,
        // nên ta dùng _visualRoot chỉ để rung — không di chuyển _sr
        // (rung _visualRoot.localPosition thay vì transform.position)
    }

    // ── Setup: tạo flash layer trắng đè lên sprite gốc ───────
    private void SetupFlashLayer()
    {
        GameObject flashObj = new GameObject("_FlashLayer");
        flashObj.transform.SetParent(_visualRoot, false);
        flashObj.transform.localPosition = Vector3.zero;
        flashObj.transform.localScale = Vector3.one;

        _flashLayer = flashObj.AddComponent<SpriteRenderer>();
        _flashLayer.sprite = _sr.sprite;
        _flashLayer.sortingLayerID = _sr.sortingLayerID;
        _flashLayer.sortingOrder = _sr.sortingOrder + 1;
        _flashLayer.color = new Color(1f, 1f, 1f, 0f); // trong suốt ban đầu
        _flashLayer.flipX = _sr.flipX;
    }

    // ── LateUpdate: đồng bộ flash layer với sprite gốc ────────
    void LateUpdate()
    {
        if (_flashLayer == null) return;
        // Đồng bộ để animation hoặc AI flip không làm lệch flash layer
        _flashLayer.flipX = _sr.flipX;
        _flashLayer.sprite = _sr.sprite;
    }

    // ── Public API ────────────────────────────────────────────

    /// <summary>
    /// Chỉ flash + knockback + shake, KHÔNG trừ HP.
    /// Dùng cho VirusAI (có HP riêng).
    /// </summary>
    public void ReactOnly(Vector2 knockbackDir)
    {
        if (_isDead) return;
        StopAllCoroutines();
        StartCoroutine(FlashWhite());
        StartCoroutine(HitShake());
        if (knockbackDir != Vector2.zero)
            StartCoroutine(ApplyKnockback(knockbackDir));
    }

    /// <summary>
    /// Flash + knockback + shake + trừ HP nội bộ.
    /// Dùng cho UtilityRobotAI (không có HP riêng).
    /// Trả về true nếu HP <= 0.
    /// </summary>
    public bool ReactToHit(Vector2 knockbackDir, float damage = 1f)
    {
        if (_isDead) return false;

        StopAllCoroutines();
        StartCoroutine(FlashWhite());
        StartCoroutine(HitShake());
        if (knockbackDir != Vector2.zero)
            StartCoroutine(ApplyKnockback(knockbackDir));

        if (!manageHp) return false;

        _currentHp -= damage;
        if (_currentHp <= 0f)
        {
            _isDead = true;
            return true;
        }
        return false;
    }

    // ── Flash trắng ───────────────────────────────────────────
    private IEnumerator FlashWhite()
    {
        // Bật ngay — trắng đục hoàn toàn
        _flashLayer.color = Color.white;
        yield return new WaitForSeconds(flashDuration);

        // Fade out mượt
        float t = 0f;
        while (t < flashFadeOutDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / flashFadeOutDuration);
            _flashLayer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        _flashLayer.color = new Color(1f, 1f, 1f, 0f);
    }

    // ── Hit Shake (rung _visualRoot.localPosition, không đụng Rigidbody2D) ──
    private IEnumerator HitShake()
    {
        if (!useShake) yield break;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Strength giảm dần theo thời gian
            float strength = Mathf.Lerp(shakeMagnitude, 0f, elapsed / shakeDuration);

            // Rung localPosition của visual root — Rigidbody2D ở parent không bị ảnh hưởng
            _visualRoot.localPosition = (Vector3)(Random.insideUnitCircle * strength);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset về đúng vị trí
        _visualRoot.localPosition = Vector3.zero;
    }

    // ── Knockback (tác động lên Rigidbody2D hoặc transform tùy enemy) ──
    private IEnumerator ApplyKnockback(Vector2 dir)
    {
        IsBeingKnockedBack = true;

        if (_rb != null)
        {
            // UtilityRobotAI: có Rigidbody2D
            _rb.linearVelocity = dir.normalized * knockbackForce;
            yield return new WaitForSeconds(knockbackDuration);
            _rb.linearVelocity = Vector2.zero;
        }
        else
        {
            // VirusAI: dùng transform.position (không có Rigidbody2D)
            float t = 0f;
            Vector2 startPos = transform.position;
            Vector2 endPos = startPos + dir.normalized * (knockbackForce * knockbackDuration);

            while (t < knockbackDuration)
            {
                t += Time.deltaTime;
                transform.position = Vector2.Lerp(startPos, endPos, t / knockbackDuration);
                yield return null;
            }
        }

        IsBeingKnockedBack = false;
    }

    public bool IsDead => _isDead;
}