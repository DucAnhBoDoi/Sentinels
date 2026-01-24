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
    public Button btnBack;
    public Color readyColor = new Color32(183, 212, 157, 255);
    public Color notReadyColor = Color.white;

    private void Awake() => Instance = this;

    private void Start()
    {
        // Gán sự kiện cho nút Back một lần duy nhất ở đây
        if(btnBack) 
        {
            btnBack.onClick.RemoveAllListeners();
            btnBack.onClick.AddListener(SteamLobby.Instance.LeaveLobby);
        }
    }

    private void OnEnable()
    {
        ResetUI();
    }

    public void ResetUI()
    {
        p1NameText.text = "Loading...";
        p1ReadyBtn.image.color = notReadyColor;
        p1ReadyBtn.interactable = false; // Khóa tạm nút P1 đến khi load xong

        // --- TRẠNG THÁI MẶC ĐỊNH (CHƯA CÓ NGƯỜI 2) ---
        if(p2Container) p2Container.SetActive(true); // Container luôn bật để chứa nút Invite
        
        // FIX: Ẩn Avatar Player 2 đi
        if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(false); 
        
        // FIX: Hiện nút Invite lên
        if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(true);
        
        // Ẩn các thông tin khác
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
        {
            SetupPlayerSlot(players[0], p1NameText, p1AvatarImage, p1ReadyBtn);
        }

        // --- PLAYER 2 ---
        if (players.Count > 1)
        {
            // --- CÓ NGƯỜI VÀO ---
            if(p2Container) p2Container.SetActive(true); 
            
            // FIX: Ẩn nút Invite
            if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(false); 

            // FIX: Hiện Avatar và Thông tin
            if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(true);
            if(p2NameText) p2NameText.gameObject.SetActive(true);
            if(p2ReadyBtn) p2ReadyBtn.gameObject.SetActive(true);
            if(p2LabelText) p2LabelText.gameObject.SetActive(true);
            
            SetupPlayerSlot(players[1], p2NameText, p2AvatarImage, p2ReadyBtn);
        }
        else
        {
            // --- KHÔNG CÓ NGƯỜI (HOẶC ĐÃ THOÁT) ---
            if(p2Container) p2Container.SetActive(true);
            
            // FIX: Hiện lại nút Invite
            if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(true);
            
            // FIX: Ẩn Avatar và Thông tin
            if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(false); 
            if(p2NameText) p2NameText.gameObject.SetActive(false);
            if(p2ReadyBtn) p2ReadyBtn.gameObject.SetActive(false);
            if(p2LabelText) p2LabelText.gameObject.SetActive(false);
        }

        // --- START BUTTON ---
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
        // Xử lý tên
        if (string.IsNullOrEmpty(player.DisplayName))
        {
            // Nếu là mình mà chưa sync kịp tên thì lấy luôn từ Steam API cho nhanh
            if(player.isLocalPlayer) nameText.text = Steamworks.SteamFriends.GetPersonaName();
            else nameText.text = "Loading...";
        }
        else
        {
            nameText.text = player.DisplayName;
        }
        
        readyBtn.image.color = player.IsReady ? readyColor : notReadyColor;
        
        // Logic nút bấm
        readyBtn.interactable = player.isLocalPlayer;

        // FIX QUAN TRỌNG CHO LẦN HOST THỨ 2:
        // Xóa sạch sự kiện cũ trước khi gán sự kiện mới
        readyBtn.onClick.RemoveAllListeners();
        
        if (player.isLocalPlayer)
        {
            // Gán lại lệnh CMD cho object Player MỚI
            readyBtn.onClick.AddListener(player.CmdToggleReady);
        }
    }

    public void OnClickStartGame()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.ServerChangeScene("GamePlayFloor1"); 
        }
    }
}