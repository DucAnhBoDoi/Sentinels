// ══════════════════════════════════════════════════════════════════════
// FILE: Assets/Scripts/Floor3/Floor3_Control.cs
// PURPOSE: Logic riêng của Floor 3 — gắn kèm với PlayerMovement.cs
//
// CÁCH DÙNG:
//   1. Gắn PlayerMovement.cs lên Player GameObject (đặt useQuestSystem = false)
//   2. Gắn Floor3_Control.cs lên CÙNG GameObject đó
//   3. Gắn PlayerHP.cs lên CÙNG GameObject đó
//   4. Xoá PlayerMovement_Level3.cs và PlayerAttack.cs khỏi tầng này
//
// PHÂN CÔNG PHÍM:
//   Player A (WASD)     : Di chuyển, J = tấn công, Space = lộn
//   Player B (Arrow)    : Di chuyển*, J = tấn công, Space = lộn
//   (*) Player B vẫn cần WASD vì PlayerMovement.cs dùng WASD chung.
//       Nếu nhóm muốn Player B dùng Arrow riêng, cần sửa PlayerMovement.cs
//       hoặc tạo phiên bản override — xem NOTE bên dưới.
//
// NOTE VỀ ARROW KEYS CHO PLAYER B:
//   PlayerMovement.cs hiện tại chỉ đọc WASD cho cả 2 player.
//   Giải pháp đơn giản nhất: thống nhất cả nhóm dùng WASD + WASD
//   (mỗi máy 1 instance game riêng), hoặc bàn với nhóm để thêm
//   enum PlayerType vào PlayerMovement.cs như file cũ của bạn.
//
// ENEMY COMPATIBILITY:
//   VirusAI           → cần implement IFloor3Damagable (xem IFloor3Damagable.cs)
//   UtilityRobotAI_Floor3 → đã có TakeDamage(), được wrap tự động ở đây
//
// CÓ THỂ XOÁ:
//   - PlayerMovement_Level3.cs  (thay bằng PlayerMovement.cs)
//   - PlayerAttack.cs           (logic attack chuyển vào đây)
//   - DebugVirusKiller.cs       (sau khi test xong)
// ══════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Scripts.Floor3.AI;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class Floor3_Control : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("Attack Settings")]
        [Tooltip("Sát thương gây ra cho VirusAI mỗi đòn.")]
        [SerializeField] private float _attackDamage = 1f;

        [Tooltip("Bán kính vùng tấn công (OverlapCircle).")]
        [SerializeField] private float _attackRange = 1.5f;

        [Tooltip("Thời gian hồi chiêu giữa các đòn đánh (giây).")]
        [SerializeField] private float _attackCooldown = 0.4f;

        [Header("Attack Key")]
        [Tooltip("Player A và B đều dùng phím J để tấn công.\n" +
                 "Space đã được PlayerMovement.cs dùng cho Roll.")]
        [SerializeField] private Key _attackKey = Key.J;

        [Tooltip("Dịch chuyển tâm vùng đánh so với vị trí player.\n" +
                 "X tự động lật theo hướng nhìn của nhân vật.\n" +
                 "Ví dụ: (0.5, 0) = đánh lệch về phía trước 0.5 unit.")]
        [SerializeField] private Vector2 _actionOffset = Vector2.zero;

        [Header("Layer Mask")]
        [Tooltip("Layer chứa các enemy (VirusAI, UtilityRobotAI). Dùng cho OverlapCircle.")]
        [SerializeField] private LayerMask _enemyLayer;

        [Header("Visual Feedback")]
        [Tooltip("(Tuỳ chọn) GameObject con nhấp nháy khi tấn công. Đặt tên AttackIndicator.")]
        [SerializeField] private GameObject _attackIndicator;
        [SerializeField] private float _indicatorDuration = 0.12f;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        // ── Private State ────────────────────────────────────────────────

        private float _cooldownTimer = 0f;
        private bool _quizActive = false;

        // ── Lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            if (_attackIndicator != null)
                _attackIndicator.SetActive(false);
        }

        private void OnEnable()
        {
            // Khoá input khi quiz đang mở
            QuizEventBus.OnQuizStarted += OnQuizStarted;
            QuizEventBus.OnQuizResolved += OnQuizResolved;
            QuizEventBus.OnTimerExpired += OnTimerExpired;
        }

        private void OnDisable()
        {
            QuizEventBus.OnQuizStarted -= OnQuizStarted;
            QuizEventBus.OnQuizResolved -= OnQuizResolved;
            QuizEventBus.OnTimerExpired -= OnTimerExpired;
        }

        // Quiz event handlers — signature phải khớp chính xác với QuizEventBus
        // OnQuizStarted  : Action<QuizQuestion>
        // OnQuizResolved : Action<bool, int>
        // OnTimerExpired : Action
        private void OnQuizStarted(QuizQuestion _) => _quizActive = true;
        private void OnQuizResolved(bool _correct, int _i) => _quizActive = false;
        private void OnTimerExpired() => _quizActive = false;

        private void Update()
        {
            // Đếm cooldown
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            // Không cho tấn công khi quiz đang chạy
            if (_quizActive) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // Đọc phím tấn công (J mặc định)
            if (kb[_attackKey].wasPressedThisFrame && _cooldownTimer <= 0f)
                PerformAttack();
        }

        // ── Attack Logic ─────────────────────────────────────────────────

        private void PerformAttack()
        {
            _cooldownTimer = _attackCooldown;

            // Flash indicator nếu có
            if (_attackIndicator != null)
                StartCoroutine(FlashIndicator());

            // Trigger animation tấn công (PlayerMovement.cs đã có Animator)
            var anim = GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("isAttacking");

            // Tính tâm vùng đánh — offset lật theo hướng nhìn của nhân vật
            float facingDir = Mathf.Sign(transform.localScale.x);
            Vector2 attackCenter = (Vector2)transform.position
                + new Vector2(_actionOffset.x * facingDir, _actionOffset.y);

            // OverlapCircle tìm enemy trong range
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                attackCenter, _attackRange, _enemyLayer);

            int hitCount = 0;

            foreach (var hit in hits)
            {
                // ── VirusAI ──────────────────────────────────────────────
                var virus = hit.GetComponent<VirusAI>();
                if (virus != null)
                {
                    virus.TakeDamage(_attackDamage);
                    hitCount++;
                    continue;
                }

                // ── UtilityRobotAI_Floor3 ────────────────────────────────
                var robot = hit.GetComponent<UtilityRobotAI_Floor3>();
                if (robot != null)
                {
                    robot.TakeDamage();
                    hitCount++;
                }
            }

            Debug.Log(hitCount > 0
                ? $"[Floor3_Control] {gameObject.name} trúng {hitCount} enemy."
                : $"[Floor3_Control] {gameObject.name} đánh hụt.");
        }

        private IEnumerator FlashIndicator()
        {
            _attackIndicator.SetActive(true);
            yield return new WaitForSeconds(_indicatorDuration);
            _attackIndicator.SetActive(false);
        }

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;
            float facingDir = Mathf.Sign(transform.localScale.x);
            Vector2 attackCenter = (Vector2)transform.position
                + new Vector2(_actionOffset.x * facingDir, _actionOffset.y);
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            Gizmos.DrawWireSphere(attackCenter, _attackRange);
        }
    }
}