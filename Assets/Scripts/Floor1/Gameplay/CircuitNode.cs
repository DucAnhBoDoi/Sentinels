using UnityEngine;

public class CircuitNode : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public bool isFixed = false;

    [Header("Đánh dấu loại Prefab")]
    public bool isWire = false;

    [Header("Gắn hình ảnh mạch điện vào đây")]
    public Sprite darkSprite;
    public Sprite glowingSprite;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = darkSprite;
        SetVisibility(false);
    }

    public void FixNode()
    {
        if (!isFixed)
        {
            isFixed = true;
            spriteRenderer.sprite = glowingSprite;
            PropagateElectricity();

            if (!isWire && PowerGridManager.Instance != null)
            {
                PowerGridManager.Instance.AddFixedNode();
            }
        }
    }

    void PropagateElectricity()
    {
        // Kiểm tra 4 ô xung quanh (trên, dưới, trái, phải) cách 1 đơn vị
        Vector2[] positionsToCheck = {
            (Vector2)transform.position + Vector2.up,
            (Vector2)transform.position + Vector2.down,
            (Vector2)transform.position + Vector2.left,
            (Vector2)transform.position + Vector2.right
        };

        foreach (Vector2 pos in positionsToCheck)
        {
            // Tìm xem có mạch điện nào ở ô đó không
            Collider2D[] colliders = Physics2D.OverlapPointAll(pos);
            foreach (Collider2D col in colliders)
            {
                CircuitNode neighbor = col.GetComponent<CircuitNode>();
                
                if (neighbor != null && neighbor != this && !neighbor.isFixed && neighbor.isWire)
                {
                    neighbor.FixNode(); 
                }
            }
        }
    }

    public void SetVisibility(bool isVisible)
    {
        Color color = spriteRenderer.color;
        color.a = isVisible ? 1f : 0f;
        spriteRenderer.color = color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Flashlight")) SetVisibility(true);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Flashlight")) SetVisibility(false);
    }
}