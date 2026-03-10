using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class FlashLightController : MonoBehaviour
{
    public Light2D Light;

    private void Awake()
    {
        Light = GetComponent<Light2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IFlashLightInteract flashLightInteract))
        {
            flashLightInteract.OnFlashLightHit(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IFlashLightInteract flashLightInteract))
        {
            flashLightInteract.OnFlashLightLeave(this);
        }
    }
}
