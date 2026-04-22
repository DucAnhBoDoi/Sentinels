using UnityEngine;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class Floor3_Control : MonoBehaviour
    {
        private PlayerMovement _playerMovement;

        private void Awake()
        {
            _playerMovement = GetComponent<PlayerMovement>();
            if (_playerMovement == null)
            {
                Debug.LogError("[Floor3_Control] Không tìm thấy PlayerMovement!");
            }
        }

        private void OnEnable()
        {
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

        // KHI BẮT ĐẦU QUIZ -> KHÓA KHÔNG CHO ĐÁNH
        private void OnQuizStarted(QuizQuestion _)
        {
            if (_playerMovement != null) _playerMovement.canAttack = false;
        }

        // KHI TRẢ LỜI XONG HOẶC HẾT GIỜ -> CHO ĐÁNH LẠI BÌNH THƯỜNG
        private void OnQuizResolved(bool _correct, int _i) => UnlockAttack();
        private void OnTimerExpired() => UnlockAttack();

        private void UnlockAttack()
        {
            if (_playerMovement != null) _playerMovement.canAttack = true; 
        }
    }
}