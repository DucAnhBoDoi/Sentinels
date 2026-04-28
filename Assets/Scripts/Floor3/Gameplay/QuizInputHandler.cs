// ============================================================
// FILE: Assets/Scripts/Floor3/Gameplay/QuizInputHandler.cs
// Namespace: Scripts.Floor3.Gameplay
// ------------------------------------------------------------
// Hỗ trợ chọn đáp án bằng bàn phím (1,2,3,4 hoặc Numpad 1,2,3,4)
// Tương thích hoàn toàn với Multiplayer QuizManager.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem;
using Scripts.Floor3.Core;

namespace Scripts.Floor3.Gameplay
{
    public class QuizInputHandler : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _logInput = true;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Bất kể là Host hay Client, cứ bấm phím là nộp đáp án của máy đó lên Server
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SubmitLocal(0);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SubmitLocal(1);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SubmitLocal(2);
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SubmitLocal(3);
        }

        private void SubmitLocal(int index)
        {
            if (QuizManager.Instance != null)
            {
                if (_logInput) Debug.Log($"[QuizInputHandler] Người chơi vừa bấm phím chọn đáp án số {index + 1}");
                QuizManager.Instance.SubmitLocalAnswer(index);
            }
        }
    }
}