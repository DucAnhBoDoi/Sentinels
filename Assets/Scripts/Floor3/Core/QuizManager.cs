// ============================================================
// FILE: Assets/Scripts/Floor3/Core/QuizManager.cs
// Namespace: Scripts.Floor3.Core
// ------------------------------------------------------------
// Orchestrates the full quiz lifecycle:
//   1. Receive StartQuiz() from Floor3Brain
//   2. Request question from IQuizGenerator
//   3. Send question to QuizUI (via QuizEventBus)
//   4. Collect answers from Player A and Player B independently
//   5. Detect conflict (players disagree)
//   6. Evaluate result → report to Floor3Brain
//   7. Handle timer expiry as wrong answer
//
// WHAT IT DOES NOT DO:
//   - Never generates questions (that's IQuizGenerator)
//   - Never draws UI (that's QuizHUDController - Day 2 UI)
//   - Never moves the robot (that's Floor3Brain's job)
//
// TWO-PLAYER CONFIRMATION RULES:
//   - Both players must submit before evaluation
//   - If they agree → evaluate immediately
//   - If they disagree → show conflict warning, wait for re-confirm
//     OR timer runs out → treat as wrong answer
//
// MULTIPLAYER NOTE:
//   Player input arrives via ServerRpc from each client.
//   Replace PlayerAnswered() calls with [ServerRpc] methods.
//   QuizEventBus events become ClientRpc broadcasts.
// ============================================================

using System.Collections;
using UnityEngine;
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.Core
{
    public class QuizManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Floor3Brain _floor3Brain;

        [Tooltip("Leave EMPTY — QuizManager auto-finds IQuizGenerator on this GameObject.\n" +
                 "Just make sure MockQuizGenerator script is on the same GameObject.\n" +
                 "Day 6: swap MockQuizGenerator → LLMQuizGenerator component, done.")]
        [SerializeField] private MonoBehaviour _quizGeneratorObject; // kept for Inspector fallback

        [Header("Timer Settings")]
        [SerializeField] private float _defaultTimeLimit = 20f;

        [Header("Conflict Settings")]
        [Tooltip("Extra seconds given after a conflict is detected for players to re-confirm")]
        [SerializeField] private float _conflictGracePeriod = 8f;

        [Header("Debug")]
        [SerializeField] private bool _logQuizFlow = true;

        // ── Private State ─────────────────────────────────────────────────

        private IQuizGenerator _generator;
        private QuizQuestion   _activeQuestion;

        private int  _playerAAnswer  = -1;   // -1 = not answered
        private int  _playerBAnswer  = -1;
        private bool _quizActive     = false;
        private bool _conflictActive = false;

        private Coroutine _timerCoroutine;

        // ── Unity Lifecycle ──────────────────────────────────────────────

        private void Awake()
        {
            // WHY GetComponent instead of Inspector cast:
            // Unity cannot reliably cast serialized MonoBehaviour fields to interfaces.
            // GetComponent<IQuizGenerator>() finds any component on this GameObject
            // that implements the interface — works for Mock, LLM, or any future generator.
            _generator = GetComponent<IQuizGenerator>();

            // Fallback: try the inspector-assigned object if GetComponent found nothing
            if (_generator == null && _quizGeneratorObject != null)
                _generator = _quizGeneratorObject as IQuizGenerator;

            if (_generator == null)
                Debug.LogError("[QuizManager] No IQuizGenerator found on this GameObject! " +
                               "Make sure MockQuizGenerator script is on the same GameObject as QuizManager.");
            else
                Debug.Log($"[QuizManager] Generator found: {_generator.GetType().Name}");

            if (_floor3Brain == null)
                Debug.LogError("[QuizManager] Floor3Brain reference is missing!");
        }

        private void OnEnable()
        {
            QuizEventBus.OnPlayerAAnswered += HandlePlayerAAnswer;
            QuizEventBus.OnPlayerBAnswered += HandlePlayerBAnswer;
        }

        private void OnDisable()
        {
            QuizEventBus.OnPlayerAAnswered -= HandlePlayerAAnswer;
            QuizEventBus.OnPlayerBAnswered -= HandlePlayerBAnswer;
        }

        // ── Public API (called by Floor3Brain / DifficultyManager) ─────────

        /// <summary>
        /// Called by DifficultyManager to adjust quiz time pressure per difficulty tier.
        /// Takes effect on the NEXT quiz started — does not affect active quiz.
        /// </summary>
        public void SetTimeLimit(float newLimit)
        {
            _defaultTimeLimit = newLimit;
            Debug.Log($"[QuizManager] Time limit updated to {newLimit}s");
        }

        // ── Public API (called by Floor3Brain) ───────────────────────────

        /// <summary>
        /// Called by Floor3Brain when a checkpoint is reached.
        /// waypointIndex is passed to the generator for context (Day 6: LLM difficulty scaling).
        /// </summary>
        public void StartQuiz(int waypointIndex)
        {
            if (_quizActive)
            {
                Debug.LogWarning("[QuizManager] StartQuiz called while quiz already active. Ignored.");
                return;
            }

            if (_generator == null)
            {
                Debug.LogError("[QuizManager] Cannot start quiz — generator is null! " +
                               "Check that MockQuizGenerator is on the same GameObject.");
                // Fail-safe: resume robot so game doesn't soft-lock
                _floor3Brain?.OnWrongAnswer();
                return;
            }

            Log($"Starting quiz for checkpoint at waypoint {waypointIndex}");
            ResetAnswers();

            _generator.RequestQuestion(
                waypointIndex,
                onComplete: OnQuestionReceived,
                onError:    OnGeneratorError
            );
        }

        // ── Question Received ────────────────────────────────────────────

        private void OnQuestionReceived(QuizQuestion question)
        {
            _activeQuestion = question;
            _quizActive     = true;
            _conflictActive = false;

            Log($"Question received: \"{question.QuestionText}\"");

            // Notify UI
            QuizEventBus.RaiseQuizStarted(question);

            // Start timer
            float timeLimit = question.TimeLimitOverride > 0f
                ? question.TimeLimitOverride
                : _defaultTimeLimit;

            _timerCoroutine = StartCoroutine(TimerCoroutine(timeLimit));
        }

        private void OnGeneratorError(string error)
        {
            Debug.LogError($"[QuizManager] Generator error: {error}. Auto-resuming robot.");
            // Fail-safe: don't soft-lock the game if generator breaks
            _floor3Brain.OnWrongAnswer();
            CleanupQuiz();
        }

        // ── Answer Collection ────────────────────────────────────────────

        private void HandlePlayerAAnswer(int answerIndex)
        {
            if (!_quizActive) return;
            _playerAAnswer = answerIndex;
            Log($"Player A answered: {answerIndex} ({_activeQuestion?.Answers[answerIndex]})");
            QuizEventBus.RaisePlayerConfirmed(PlayerSlot.PlayerA, answerIndex);
            TryEvaluate();
        }

        private void HandlePlayerBAnswer(int answerIndex)
        {
            if (!_quizActive) return;
            _playerBAnswer = answerIndex;
            Log($"Player B answered: {answerIndex} ({_activeQuestion?.Answers[answerIndex]})");
            QuizEventBus.RaisePlayerConfirmed(PlayerSlot.PlayerB, answerIndex);
            TryEvaluate();
        }

        // ── Evaluation Logic ─────────────────────────────────────────────

        private void TryEvaluate()
        {
            // Wait until both players have answered
            if (_playerAAnswer == -1 || _playerBAnswer == -1) return;

            if (_playerAAnswer == _playerBAnswer)
            {
                // Agreement — evaluate immediately
                _conflictActive = false;
                EvaluateFinalAnswer(_playerAAnswer);
            }
            else
            {
                // Conflict — players disagree
                if (!_conflictActive)
                {
                    _conflictActive = true;
                    Log($"CONFLICT: A chose {_playerAAnswer}, B chose {_playerBAnswer}. Grace period started.");
                    QuizEventBus.RaiseConflictDetected(_playerAAnswer, _playerBAnswer);

                    // Reset answers so they must re-confirm
                    ResetAnswers();

                    // Extend timer by grace period
                    StopTimerCoroutine();
                    _timerCoroutine = StartCoroutine(TimerCoroutine(_conflictGracePeriod));
                }
            }
        }

        private void EvaluateFinalAnswer(int chosenIndex)
        {
            StopTimerCoroutine();

            bool isCorrect = (chosenIndex == _activeQuestion.CorrectAnswerIndex);
            Log($"Answer evaluated: {(isCorrect ? "CORRECT ✓" : "WRONG ✗")} " +
                $"(chosen={chosenIndex}, correct={_activeQuestion.CorrectAnswerIndex})");

            QuizEventBus.RaiseQuizResolved(isCorrect, _activeQuestion.CorrectAnswerIndex);

            if (isCorrect)
                _floor3Brain.OnCorrectAnswer();
            else
                _floor3Brain.OnWrongAnswer();

            CleanupQuiz();
        }

        // ── Timer ─────────────────────────────────────────────────────────

        private IEnumerator TimerCoroutine(float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // CRITICAL: Use unscaledDeltaTime so the quiz timer continues
                // counting while Time.timeScale = 0 (game frozen during quiz).
                // If we used Time.deltaTime here, the timer would freeze too
                // and the quiz would never time out.
                elapsed += Time.unscaledDeltaTime;
                float normalized = 1f - (elapsed / duration);      // 1 → 0
                QuizEventBus.RaiseTimerTick(normalized, duration - elapsed);
                yield return null;
            }

            Log("Timer expired — counting as wrong answer.");
            QuizEventBus.RaiseTimerExpired();
            EvaluateFinalAnswer(-1);    // -1 = timeout, always wrong
        }

        private void StopTimerCoroutine()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        // ── Cleanup ───────────────────────────────────────────────────────

        private void CleanupQuiz()
        {
            _quizActive     = false;
            _conflictActive = false;
            _activeQuestion = null;
            ResetAnswers();
            StopTimerCoroutine();
        }

        private void ResetAnswers()
        {
            _playerAAnswer = -1;
            _playerBAnswer = -1;
        }

        private void Log(string msg)
        {
            if (_logQuizFlow) Debug.Log($"[QuizManager] {msg}");
        }
    }
}