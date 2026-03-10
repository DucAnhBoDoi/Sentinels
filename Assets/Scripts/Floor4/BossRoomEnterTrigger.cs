using UnityEngine;

public class BossRoomEnterTrigger : MonoBehaviour
{
    [SerializeField]
    private GameObject _gate;

    [SerializeField]
    private BossController _boss;

    [SerializeField]
    private GameObject _bossAvatar;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(nameof(GameLayerMask.Player)))
        {
            _gate.SetActive(true);
            _boss.gameObject.SetActive(true);
            Destroy(_bossAvatar);
            Destroy(gameObject);
        }
    }
}
