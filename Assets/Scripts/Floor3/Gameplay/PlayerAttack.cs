// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/PlayerAttack.cs
// Namespace: Scripts.Floor3.Gameplay
// ── DAY 5 ──────────────────────────────────────────────────
// Production attack system for Player B (and optionally A).
// Replaces DebugVirusKiller.
//
// HOW IT WORKS:
//   Player B presses SPACE → melee swing in facing direction
//   Overlap circle check → hits VirusAI in range → deals damage
//   Visual: AttackIndicator GameObject briefly appears
//   Cooldown: prevents spam
//
// PLAYER A: Press F key → smaller range "push" attack (optional)
//
// DESIGN:
//   - Attach to Player_A_Navigator AND Player_B_Mechanic
//   - Set PlayerType in Inspector (same as PlayerMovement)
//   - Never references QuizManager or Floor3Brain
//   - Calls VirusAI.TakeDamage() directly — virus decides if it dies
//
// MULTIPLAYER NOTE:
//   Wrap attack logic with [ServerRpc] from the attacking client.
//   AttackIndicator shown locally, damage applied server-side.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;
using Scripts.Floor3.AI;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class PlayerAttack : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        public enum AttackPlayerType { PlayerA, PlayerB }

        [Header("Player Setup")]
        [SerializeField] private AttackPlayerType _playerType = AttackPlayerType.PlayerB;

        [Header("Attack Settings")]
        [SerializeField] private float _attackDamage = 1f;
        [SerializeField] private float _attackRange = 1.5f;
        [SerializeField] private float _attackCooldown = 0.4f;

        [Header("Visual Feedback")]
        [Tooltip("Optional child GameObject that flashes briefly on attack.\n" +
                 "Create a sprite circle, set alpha low, name it AttackIndicator.")]
        [SerializeField] private GameObject _attackIndicator;
        [SerializeField] private float _indicatorDuration = 0.12f;

        [Header("Layer Mask")]
        [Tooltip("Layer the virus prefabs are on. Used for OverlapCircle.")]
        [SerializeField] private LayerMask _virusLayer;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        // ── Private State ─────────────────────────────────────────────────

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
            // Disable attack input during quiz
            QuizEventBus.OnQuizStarted += _ => _quizActive = true;
            QuizEventBus.OnQuizResolved += (_c, _i) => _quizActive = false;
            QuizEventBus.OnTimerExpired += () => _quizActive = false;
        }

        private void OnDisable()
        {
            QuizEventBus.OnQuizStarted -= _ => _quizActive = true;
            QuizEventBus.OnQuizResolved -= (_c, _i) => _quizActive = false;
            QuizEventBus.OnTimerExpired -= () => _quizActive = false;
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;

            if (_quizActive) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            bool attackPressed = _playerType == AttackPlayerType.PlayerB
                ? kb.spaceKey.wasPressedThisFrame
                : kb.fKey.wasPressedThisFrame;

            if (attackPressed && _cooldownTimer <= 0f)
                PerformAttack();
        }

        // ── Attack Logic ─────────────────────────────────────────────────

        private void PerformAttack()
        {
            _cooldownTimer = _attackCooldown;

            // Flash visual indicator
            if (_attackIndicator != null)
                StartCoroutine(FlashIndicator());

            // OverlapCircle — find all viruses in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position, _attackRange, _virusLayer);

            int killCount = 0;
            foreach (var hit in hits)
            {
                // ── Virus ─────────────────────────────
                var virus = hit.GetComponent<VirusAI>();
                if (virus != null)
                {
                    virus.TakeDamage(_attackDamage);
                    killCount++;
                    continue; // tránh check trùng
                }

                // ── Utility Robot ─────────────────────
                var robot = hit.GetComponent<UtilityRobotAI_Floor3>();
                if (robot != null)
                {
                    robot.TakeDamage(); // robot của bạn không có damage param
                    killCount++;
                }
            }

            if (killCount > 0)
                Debug.Log($"[PlayerAttack] {gameObject.name} hit {killCount} enemy(s).");
            else
                Debug.Log($"[PlayerAttack] {gameObject.name} swung — missed.");
        }

        private System.Collections.IEnumerator FlashIndicator()
        {
            _attackIndicator.SetActive(true);
            yield return new WaitForSeconds(_indicatorDuration);
            _attackIndicator.SetActive(false);
        }

        // ── Gizmos ───────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }
    }
}