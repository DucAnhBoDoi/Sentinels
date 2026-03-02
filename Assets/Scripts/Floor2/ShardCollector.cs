using UnityEngine;

public class ShardCollector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem đối tượng chạm vào có Tag là "Player" hay không
        if (other.CompareTag("Player")) 
        {
            Debug.Log(other.name + " đã nhặt được mảnh vỡ phần thưởng!");
            
            // Tìm GameManager để thực hiện các hiệu ứng kết thúc màn chơi (nếu cần)
            GameManager gm = Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                // Bạn có thể thêm lệnh chuyển cảnh hoặc cộng điểm ở đây
            }

            // Xóa mảnh vỡ khỏi bản đồ sau khi nhặt
            Destroy(gameObject); 
        }
    }
}