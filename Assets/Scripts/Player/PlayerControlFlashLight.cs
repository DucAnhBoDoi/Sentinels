using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PlayerController))]
public class PlayerControlFlashLight : MonoBehaviour
{
    private PlayerController _controller;

    private int flashLightColorIdx;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (_controller.Stats.FlashLightColors.Length > 0)
        {
            _controller.FlashLight.color = _controller.Stats.FlashLightColors[0];
            flashLightColorIdx = 0;
        }
        else
        {
            flashLightColorIdx = -1;
        }
    }

    private void Update()
    {
        Light2D flashLight = _controller.FlashLight;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            flashLight.gameObject.SetActive(!flashLight.gameObject.activeSelf);
        }
        else if (Keyboard.current.cKey.wasPressedThisFrame && flashLightColorIdx != -1)
        {
            flashLightColorIdx = (flashLightColorIdx + 1) % _controller.Stats.FlashLightColors.Length;
            flashLight.color = _controller.Stats.FlashLightColors[flashLightColorIdx];
        }
    }
}
