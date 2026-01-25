using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;
using System.Collections.Generic;
using Steamworks;

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
        if(btnBack) 
        {
            btnBack.onClick.RemoveAllListeners();
            // Đảm bảo Instance không null
            if (SteamLobby.Instance != null)
            {
                btnBack.onClick.AddListener(SteamLobby.Instance.LeaveLobby);
            }
        }
    }

    private void OnEnable()
    {
        ResetUI();
    }

    public void ResetUI()
    {
        if(p1NameText) p1NameText.text = "Loading...";
        if(p1ReadyBtn) 
        {
            p1ReadyBtn.image.color = notReadyColor;
            p1ReadyBtn.interactable = false;
        }

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
        // --- FIX WARNING Ở ĐÂY ---
        // Thay FindObjectsOfType bằng FindObjectsByType(FindObjectsSortMode.None)
        // Nó nhanh hơn vì không cần sắp xếp mặc định của Unity, ta sẽ tự sort theo netId bên dưới
        List<PlayerLobby> players = FindObjectsByType<PlayerLobby>(FindObjectsSortMode.None)
                                    .OrderBy(x => x.netId)
                                    .ToList();

        // --- PLAYER 1 ---
        if (players.Count > 0)
        {
            SetupPlayerSlot(players[0], p1NameText, p1AvatarImage, p1ReadyBtn);
        }

        // --- PLAYER 2 ---
        if (players.Count > 1)
        {
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
            if(p2Container) p2Container.SetActive(true);
            if(p2InviteBtn) p2InviteBtn.gameObject.SetActive(true);
            
            if(p2AvatarImage) p2AvatarImage.gameObject.SetActive(false); 
            if(p2NameText) p2NameText.gameObject.SetActive(false);
            if(p2ReadyBtn) p2ReadyBtn.gameObject.SetActive(false);
            if(p2LabelText) p2LabelText.gameObject.SetActive(false);
        }

        if (NetworkServer.active && btnStart != null) 
        {
            btnStart.gameObject.SetActive(true);
            bool canStart = players.Count == 2 && players.All(p => p.IsReady);
            btnStart.interactable = canStart;
        }
        else if(btnStart != null)
        {
            btnStart.gameObject.SetActive(false);
        }
    }

    void SetupPlayerSlot(PlayerLobby player, TMP_Text nameText, Image avatar, Button readyBtn)
    {
        if (nameText == null || readyBtn == null) return;

        if (string.IsNullOrEmpty(player.DisplayName))
        {
            if(player.isLocalPlayer && SteamManager.Initialized) 
                nameText.text = SteamFriends.GetPersonaName();
            else 
                nameText.text = "Loading...";
        }
        else
        {
            nameText.text = player.DisplayName;
        }
        
        readyBtn.image.color = player.IsReady ? readyColor : notReadyColor;
        readyBtn.interactable = player.isLocalPlayer;

        readyBtn.onClick.RemoveAllListeners();
        
        if (player.isLocalPlayer)
        {
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