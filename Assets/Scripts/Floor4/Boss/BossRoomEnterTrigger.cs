using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class BossRoomEnterTrigger : NetworkBehaviour
{
    [SerializeField] private GameObject _gate;

    // [SerializeField] private BossController _boss;

    // [SerializeField] private GameObject _bossAvatar;

    [SerializeField] private BossPhase1 _bossPhase1;
    [SerializeField] private BossPhase1 _bossPhase2;
    [SerializeField] private float _bossPhase1DeathDuration;

    private void Awake()
    {
        _bossPhase1.enabled = false;
        _bossPhase1.OnDeath += OnBossPhase1Death;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }

        _gate.SetActive(true);
        // _boss.gameObject.SetActive(true);
        // Destroy(_bossAvatar);
        _bossPhase1.enabled = true;

        // --- THÊM LỆNH: BẬT NHẠC BOSS KHI ĐÓNG CỬA ---
        PlayBossMusic();

        if (NetworkManager && NetworkManager.IsClient)
        {
            TeleportPlayersClientRpc();
        }
        else
        {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
            {
                player.transform.position = transform.position;
            }
        }

        gameObject.SetActive(false);
    }

    private void OnBossPhase1Death()
    {
        DOTween.Sequence()
            .AppendInterval(_bossPhase1DeathDuration)
            .OnComplete(() =>
            {
                _bossPhase1.transform.parent.gameObject.SetActive(false);
                if (NetworkManager.IsServer)
                {
                    _bossPhase2.transform.parent.position = Vector2.zero;
                    _bossPhase2.enabled = true;
                }
                // _bossPhase2Root.Spawn(true);
                // _bossPhase2.NetworkObject.Spawn(true);
                // _bossPhase2.NetworkObject.TrySetParent(_bossPhase2Root);
            });
        _bossPhase1.OnDeath = null;
    }

    [ClientRpc]
    private void TeleportPlayersClientRpc()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            player.transform.position = transform.position;
        }

        // --- ĐẢM BẢO CÁC MÁY BỊ DỊCH CHUYỂN VÀO CŨNG NGHE THẤY NHẠC ---
        PlayBossMusic();
    }

    // --- HÀM TỰ VIẾT ĐỂ TÌM VÀ BẬT NHẠC (KHÔNG PHÁ CẤU TRÚC GỐC) ---
    private void PlayBossMusic()
    {
        GameObject bgmManager = GameObject.Find("BGM_Manager");
        if (bgmManager != null)
        {
            AudioSource bgmSource = bgmManager.GetComponent<AudioSource>();
            if (bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }
    }
}