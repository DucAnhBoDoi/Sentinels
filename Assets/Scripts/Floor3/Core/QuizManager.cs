using System.Collections;
using UnityEngine;
using Unity.Netcode; // THÊM THƯ VIỆN MẠNG
using Scripts.Floor3.Gameplay;

namespace Scripts.Floor3.Core
{
    // ĐỔI TỪ MonoBehaviour SANG NetworkBehaviour
    public class QuizManager : NetworkBehaviour
    {
        public static QuizManager Instance; // Thêm Singleton để UI dễ gọi

        [Header("References")]
        [SerializeField] private Floor3Brain _floor3Brain;
        [SerializeField] private MonoBehaviour _quizGeneratorObject;

        [Header("Timer Settings")]
        [SerializeField] private float _defaultTimeLimit = 20f;

        [Header("Conflict Settings")]
        [SerializeField] private float _conflictGracePeriod = 8f;

        [Header("Debug")]
        [SerializeField] private bool _logQuizFlow = true;

        private IQuizGenerator _generator;

        // Server giữ câu hỏi thật (có đáp án đúng), Client chỉ giữ câu hỏi ảo để hiển thị
        private QuizQuestion _serverSideQuestion;

        private int _playerAAnswer = -1;
        private int _playerBAnswer = -1;
        private bool _quizActive = false;
        private bool _conflictActive = false;

        private Coroutine _timerCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this; // Khởi tạo Singleton

            _generator = GetComponent<IQuizGenerator>();
            if (_generator == null && _quizGeneratorObject != null)
                _generator = _quizGeneratorObject as IQuizGenerator;

            if (_generator == null)
                Debug.LogError("[QuizManager] No IQuizGenerator found!");
            else
                Debug.Log($"[QuizManager] Generator found: {_generator.GetType().Name}");

            if (_floor3Brain == null)
                Debug.LogError("[QuizManager] Floor3Brain reference is missing!");
        }

        // --- THÊM MỚI: Reset sạch sẽ mọi thứ khi Load lại Scene (Restart) ---
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            CleanupQuiz();
            // Bắt buộc phải chọn lại Topic khi Restart màn chơi
            Scripts.Floor3.UI.TopicSelectionUI.hasStartedMission = false; 
        }

        public void SetTimeLimit(float newLimit)
        {
            _defaultTimeLimit = newLimit;
            Debug.Log($"[QuizManager] Time limit updated to {newLimit}s");
        }

        public void StartQuiz(int waypointIndex)
        {
            // CHỈ SERVER MỚI ĐƯỢC PHÉP BẮT ĐẦU QUIZ
            if (!IsServer) return;

            if (_quizActive) return;

            if (_generator == null)
            {
                _floor3Brain?.OnWrongAnswer();
                return;
            }

            Log($"Starting quiz for checkpoint at waypoint {waypointIndex}");
            ResetAnswers();

            _generator.RequestQuestion(
                waypointIndex,
                onComplete: OnQuestionReceived,
                onError: OnGeneratorError
            );
        }

        private void OnQuestionReceived(QuizQuestion question)
        {
            _serverSideQuestion = question; // Server giữ đáp án đúng

            float timeLimit = question.TimeLimitOverride > 0f ? question.TimeLimitOverride : _defaultTimeLimit;

            // PHÁT THANH CÂU HỎI CHO TOÀN BỘ CLIENT CÙNG HIỂN THỊ
            StartQuizClientRpc(
                question.QuestionText,
                question.Answers[0],
                question.Answers[1],
                question.Answers[2],
                question.Answers[3],
                timeLimit
            );
        }

        [ClientRpc]
        private void StartQuizClientRpc(string qText, string a0, string a1, string a2, string a3, float timeLimit)
        {
            // Reconstruct câu hỏi trên cả Host và Client để truyền cho UI
            QuizQuestion displayQuestion = new QuizQuestion();
            displayQuestion.QuestionText = qText;
            displayQuestion.Answers = new string[] { a0, a1, a2, a3 };
            displayQuestion.TimeLimitOverride = timeLimit;

            _quizActive = true;
            _conflictActive = false;

            Log($"Question UI Synced: \"{qText}\"");

            QuizEventBus.RaiseQuizStarted(displayQuestion);

            StopTimerCoroutine();
            _timerCoroutine = StartCoroutine(TimerCoroutine(timeLimit));
        }

        private void OnGeneratorError(string error)
        {
            Debug.LogError($"[QuizManager] Generator error: {error}. Auto-resuming robot.");
            _floor3Brain.OnWrongAnswer();
            CleanupQuiz();
        }

        // =========================================================
        // HÀM NHẬN LỆNH CLICK CHUỘT TỪ UI (VÁ LỖI CÚ PHÁP)
        // =========================================================
        public void SubmitLocalAnswer(int index)
        {
            if (!_quizActive) return;
            Log($"Sending answer {index} to server...");

            // Cú pháp chuẩn nhất để Client gọi Server mà không cần quyền Ownership
            SubmitAnswerServerRpc(index);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitAnswerServerRpc(int answerIndex, RpcParams rpcParams = default)
        {
            if (!_quizActive) return;

            // Nhận diện ai vừa click dựa vào ID mạng
            ulong senderId = rpcParams.Receive.SenderClientId;
            bool isHost = (senderId == NetworkManager.ServerClientId);

            if (isHost)
            {
                _playerAAnswer = answerIndex;
                Log($"Player A (Host) selected answer {answerIndex}");
            }
            else
            {
                _playerBAnswer = answerIndex;
                Log($"Player B (Client) selected answer {answerIndex}");
            }

            // Báo cho mọi màn hình biết để bôi màu ô được chọn
            PlayerAnsweredClientRpc(isHost, answerIndex);

            TryEvaluate(); // Kiểm tra xem cả 2 đã chọn xong chưa
        }

        [ClientRpc]
        private void PlayerAnsweredClientRpc(bool isPlayerA, int answerIndex)
        {
            if (isPlayerA) QuizEventBus.RaisePlayerConfirmed(PlayerSlot.PlayerA, answerIndex);
            else QuizEventBus.RaisePlayerConfirmed(PlayerSlot.PlayerB, answerIndex);
        }

        private void TryEvaluate()
        {
            if (!IsServer) return; // Chỉ Server mới được phép chấm điểm

            if (_playerAAnswer == -1 || _playerBAnswer == -1) return;

            if (_playerAAnswer == _playerBAnswer)
            {
                _conflictActive = false;
                EvaluateFinalAnswer(_playerAAnswer);
            }
            else
            {
                if (!_conflictActive)
                {
                    Log($"CONFLICT! Grace period started.");
                    ConflictDetectedClientRpc(_playerAAnswer, _playerBAnswer);
                }
            }
        }

        [ClientRpc]
        private void ConflictDetectedClientRpc(int ansA, int ansB)
        {
            _conflictActive = true;
            QuizEventBus.RaiseConflictDetected(ansA, ansB);
            ResetAnswers();
            StopTimerCoroutine();
            _timerCoroutine = StartCoroutine(TimerCoroutine(_conflictGracePeriod));
        }

        private void EvaluateFinalAnswer(int chosenIndex)
        {
            if (!IsServer) return;
            StopTimerCoroutine();

            // Chấm điểm dựa trên câu hỏi gốc của Server
            bool isCorrect = (chosenIndex == _serverSideQuestion.CorrectAnswerIndex);

            if (isCorrect) _floor3Brain.OnCorrectAnswer();
            else _floor3Brain.OnWrongAnswer();

            // Báo kết quả cho mọi người
            QuizResolvedClientRpc(isCorrect, _serverSideQuestion.CorrectAnswerIndex);
        }

        [ClientRpc]
        private void QuizResolvedClientRpc(bool isCorrect, int correctIndex)
        {
            StopTimerCoroutine();
            QuizEventBus.RaiseQuizResolved(isCorrect, correctIndex);
            CleanupQuiz();
        }

        private IEnumerator TimerCoroutine(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = 1f - (elapsed / duration);
                QuizEventBus.RaiseTimerTick(normalized, duration - elapsed);
                yield return null;
            }

            if (IsServer) // Chỉ Server mới có quyền chốt Hết Giờ
            {
                Log("Timer expired — counting as wrong answer.");
                QuizEventBus.RaiseTimerExpired();
                EvaluateFinalAnswer(-1);
            }
        }

        private void StopTimerCoroutine()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private void CleanupQuiz()
        {
            _quizActive = false;
            _conflictActive = false;
            _serverSideQuestion = null;
            ResetAnswers();
            StopTimerCoroutine();
        }

        private void ResetAnswers()
        {
            _playerAAnswer = -1;
            _playerBAnswer = -1;
        }

        private void Log(string msg) { if (_logQuizFlow) Debug.Log($"[QuizManager] {msg}"); }
    }
}