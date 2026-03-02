using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightControl : MonoBehaviour
{
    [Header("Object chứa Spot Light 2D")]
    public Transform flashlightTransform;

    void Start()
    {
        if (flashlightTransform != null)
        {
            flashlightTransform.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Bật/Tắt đèn pin bằng phím F
        if (keyboard.fKey.wasPressedThisFrame && flashlightTransform != null)
        {
            bool isLightOn = flashlightTransform.gameObject.activeSelf;
            flashlightTransform.gameObject.SetActive(!isLightOn);
        }

        // Xoay đèn pin theo hướng chuột
        if (flashlightTransform != null && flashlightTransform.gameObject.activeSelf && Mouse.current != null && Camera.main != null)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Camera.main.nearClipPlane));
            mouseWorldPosition.z = 0f; 

            Vector3 lookDirection = mouseWorldPosition - flashlightTransform.position;
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            
            flashlightTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }
}