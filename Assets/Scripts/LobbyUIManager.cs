using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance;

    [Header("--- PLAYER 1 SETUP ---")]
    public TMP_Text p1NameText;
    public Image p1AvatarImage;
    public Button p1ReadyBtn;

    [Header("--- PLAYER 2 SETUP ---")]
    public GameObject p2Container; 
    public Image p2AvatarImage;      
    public Button p2InviteBtn;       
    public TMP_Text p2NameText;      
    public Button p2ReadyBtn;        
    public TMP_Text p2LabelText;     

    [Header("--- GENERAL ---")]
    public Button btnStart;
    public Button btnBack; // <-- Bắt buộc phải kéo nút Back vào đây
    
    public Color readyColor = new Color32(183, 212, 157, 255);
    public Color notReadyColor = Color.white;

    private void Awake() => Instance = this;

    // CHẠY MỖI KHI BẬT LẠI UI -> ĐẢM BẢO NÚT BẤM LUÔN MỚI
    private void OnEnable()
    {
        ResetUI();
        BindButtonEvents();
    }

    void BindButtonEvents()
    {
        // Gắn sự kiện cho nút Back
        if (btnBack != null)
        {
            btnBack.onClick.RemoveAllListeners(); // Xóa lệnh cũ
            btnBack.onClick.AddListener(() => 
            {
                if (SteamLobby.Instance != null)
                    SteamLobby.Instance.LeaveLobby();
                else
                    gameObject.SetActive(false); // Fallback nếu lỗi
            });
        }

        // Gắn sự kiện cho nút Invite
        if (p2InviteBtn != null)
        {
            p2InviteBtn.onClick.RemoveAllListeners();
            p2InviteBtn.onClick.AddListener(() => 
            {
                if (SteamLobby.Instance != null) SteamLobby.Instance.InviteFriends();
            });
        }
    }

    public void ResetUI()
    {
        p1NameText.text = "Loading...";
        p1ReadyBtn.image.color = notReadyColor;
        p1ReadyBtn.interactable = false;

        // Reset P2 về trạng thái: Chỉ hiện khung Invite
        if(p2Container) p2Container.SetActive(true);
        if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(false);
        if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(true);
        if(p2NameText) p2NameText.gameObject.SetActive(false);
        if(p2ReadyBtn) p2ReadyBtn.gameObject.SetActive(false);
        if(p2LabelText) p2LabelText.gameObject.SetActive(false);

        if(btnStart) btnStart.gameObject.SetActive(false);
    }

    public void UpdateUI()
    {
        List<PlayerLobby> players = FindObjectsOfType<PlayerLobby>().OrderBy(x => x.netId).ToList();

        // --- PLAYER 1 ---
        if (players.Count > 0)
            SetupPlayerSlot(players[0], p1NameText, p1AvatarImage, p1ReadyBtn);

        // --- PLAYER 2 ---
        if (players.Count > 1)
        {
            // Có người -> Ẩn Invite, Hiện Avatar/Tên
            if(p2Container) p2Container.SetActive(true);
            if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(false);
            
            if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(true);
            if(p2NameText) p2NameText.gameObject.SetActive(true);
            if(p2ReadyBtn) p2ReadyBtn.gameObject.SetActive(true);
            if(p2LabelText) p2LabelText.gameObject.SetActive(true);
            
            SetupPlayerSlot(players[1], p2NameText, p2AvatarImage, p2ReadyBtn);
        }
        else
        {
            // Không có người -> Reset lại
            if(p2Container) p2Container.SetActive(true);
            if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(true);
            
            if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(false);
            if(p2NameText) p2NameText.gameObject.SetActive(false);
            if(p2ReadyBtn) p2ReadyBtn.gameObject.SetActive(false);
            if(p2LabelText) p2LabelText.gameObject.SetActive(false);
        }

        // --- START BTN ---
        if (NetworkServer.active) 
        {
            btnStart.gameObject.SetActive(true);
            bool canStart = players.Count == 2 && players.All(p => p.IsReady);
            btnStart.interactable = canStart;
        }
        else
        {
            btnStart.gameObject.SetActive(false);
        }
    }

    void SetupPlayerSlot(PlayerLobby player, TMP_Text nameText, Image avatar, Button readyBtn)
    {
        if (string.IsNullOrEmpty(player.DisplayName))
            nameText.text = player.isLocalPlayer ? Steamworks.SteamFriends.GetPersonaName() : "Loading...";
        else
            nameText.text = player.DisplayName;
        
        readyBtn.image.color = player.IsReady ? readyColor : notReadyColor;
        readyBtn.interactable = player.isLocalPlayer;

        // Xóa sự kiện cũ để tránh lỗi Host lần 2
        readyBtn.onClick.RemoveAllListeners();
        if (player.isLocalPlayer)
        {
            readyBtn.onClick.AddListener(player.CmdToggleReady);
        }
    }

    public void OnClickStartGame()
    {
        if (NetworkServer.active)
            NetworkManager.singleton.ServerChangeScene("GamePlayFloor1"); 
    }
}