using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayOptionsManager : MonoBehaviour
{
    [Header("Kéo Prefab Popup Options vào đây")]
    public GameObject optionsPanel;

    void Start()
    {
        // Đảm bảo lúc mới vào game thì bảng này bị tắt
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Nếu bấm phím ESC (Escape)
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (optionsPanel != null)
            {
                // Tắt thì bật lên, đang bật thì tắt đi
                bool isActive = optionsPanel.activeSelf;
                optionsPanel.SetActive(!isActive);
            }
        }
    }
}