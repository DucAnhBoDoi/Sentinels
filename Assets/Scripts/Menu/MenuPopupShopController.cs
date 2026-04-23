using UnityEngine;
using UnityEngine.UI;
using TMPro; // Dùng cho TextMeshPro
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;

public class MenuPopupShopController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private MenuSceneUtils _utils;
    [SerializeField] private ShopServerSO _shopServer;
    [SerializeField] private Button _btnBuyCoin;
    [SerializeField] private Button _btnClose;

    [Header("Menu Background")]
    public Image realMenuBackground;     // Kéo cái BackgroundMenu ở ngoài Hierarchy vào đây

    [Header("Shop UI Elements (Kéo thả vào đây)")]
    public TextMeshProUGUI txtCoin;      // Chữ "Coin: 54500" ở góc phải
    public Image previewImage;           // Cái hình màu trắng to đùng ở cột phải
    public Button btnAction;             // Nút Buy/Equip (Buy&EquipPopup > BtnBuy)
    public TextMeshProUGUI txtBtnAction; // Chữ bên trong cái nút đó

    [Header("Shop Data")]
    public ShopItemSO[] allItems;        // Kéo 4 cái file dữ liệu Item_BG... vào đây
    public ShopItemUI[] itemUIs;         // Kéo 6 cái Item_1 -> Item_6 trên Hierarchy vào đây

    private string currentUserId;
    private ShopItemUI currentSelectedItem;

    // --- Biến lưu trữ dữ liệu của User ---
    private int userCoin = 0;
    private List<string> userInventory = new List<string>();
    private string equippedBG = "BG_Menu";
    private string equippedEffect = "NONE";

    private void Awake()
    {
        currentUserId = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserId));
    }

    private void Start()
    {
        _btnBuyCoin.onClick.AddListener(OnBtnBuyCoinClick);
        _btnClose.onClick.AddListener(OnBtnCloseClick);

        // Lắng nghe sự kiện bấm cái nút bự bên phải
        btnAction.onClick.AddListener(OnBtnActionClick);

        Debug.Log("Start - User ID hiện tại là: " + currentUserId);

        // Tự động đổ dữ liệu SO vào các ô Item
        for (int i = 0; i < itemUIs.Length; i++)
        {
            if (i < allItems.Length)
            {
                itemUIs[i].gameObject.SetActive(true);
                itemUIs[i].Setup(allItems[i], this); // Truyền data vào
            }
            else
            {
                // Nếu mình có ít đồ bán hơn số ô UI thì ẩn ô dư đi
                itemUIs[i].gameObject.SetActive(false);
            }
        }

        // Tự động chọn món đồ đầu tiên khi mở Shop
        if (itemUIs.Length > 0 && itemUIs[0].gameObject.activeSelf)
        {
            OnItemSelected(itemUIs[0]);
        }
    }

    private void OnEnable()
    {
        // 1. Lấy ID hiện tại (Có thể rỗng nếu chưa đăng nhập)
        currentUserId = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserId));

        // 2. CHỈ GỌI API KHI ĐÃ CÓ ID
        if (!string.IsNullOrEmpty(currentUserId))
        {
            StartCoroutine(FetchUserData());
        }
        else
        {
            userCoin = 0;
            txtCoin.text = "0";
            userInventory.Clear();
            equippedBG = "BG_Menu";
            equippedEffect = "NONE";
            ApplyBackgroundToMenu(equippedBG);
            UpdateButtonState();
        }
    }

    public void ReloadShopData()
    {
        // Lấy ID mới nhất vừa được đăng nhập
        currentUserId = PlayerPrefs.GetString(nameof(PlayerPrefsKeys.S_UserId));

        // Nếu có ID thì lập tức gọi Server tải kho đồ và đổi ảnh nền
        if (!string.IsNullOrEmpty(currentUserId))
        {
            StartCoroutine(FetchUserData());
        }
    }

    // --- HÀM NÀY ĐỂ XÓA SẠCH DỮ LIỆU KHI ĐĂNG XUẤT ---
    public void ResetShopData()
    {
        // 1. Xóa ID hiện tại
        currentUserId = "";

        // 2. Trả tiền và kho đồ về số 0
        userCoin = 0;
        txtCoin.text = "0";
        userInventory.Clear();

        // 3. Lột đồ, trả về hình nền mặc định ban đầu
        equippedBG = "BG_Menu";
        equippedEffect = "NONE";

        // Đổi ngay lập tức cái ảnh nền ở ngoài Menu
        ApplyBackgroundToMenu(equippedBG);

        // 4. Cập nhật lại nút bấm (thành chữ Buy hết)
        UpdateButtonState();

        Debug.Log("Đã reset toàn bộ Shop về trạng thái khách (Guest)!");
    }

    public void OnItemSelected(ShopItemUI itemUI)
    {
        currentSelectedItem = itemUI;

        // Hiện hình to bên phải
        previewImage.sprite = itemUI.itemData.backgroundSprite;

        // Cập nhật lại nút bấm (Xem phải hiện chữ Buy hay Equip)
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (currentSelectedItem == null) return;

        string id = currentSelectedItem.itemData.itemId;
        string category = currentSelectedItem.itemData.category;

        // Kiểm tra xem User đã có món này chưa
        bool isOwned = userInventory.Contains(id) || id == "BG_Menu";

        // Kiểm tra xem có đang xài món này không
        bool isEquipped = (category == "Background" && equippedBG == id) ||
                          (category == "Effect" && equippedEffect == id);

        // Mở khóa nút
        btnAction.interactable = true;

        if (isEquipped)
        {
            txtBtnAction.text = "Equipped";
            btnAction.interactable = false; // Đang mặc rồi thì làm mờ nút, không cho bấm nữa
        }
        else if (isOwned)
        {
            txtBtnAction.text = "Equip"; // Đã mua nhưng chưa xài -> Đổi thành Equip
        }
        else
        {
            txtBtnAction.text = "Buy"; // Chưa có thì gạ mua
        }
    }

    private void OnBtnActionClick()
    {
        if (currentSelectedItem == null) return;

        string id = currentSelectedItem.itemData.itemId;
        bool isOwned = userInventory.Contains(id) || id == "BG_Menu";

        if (isOwned)
        {
            StartCoroutine(EquipItemRoutine(id, currentSelectedItem.itemData.category));
        }
        else
        {
            StartCoroutine(BuyItemRoutine(id, currentSelectedItem.itemData.category, currentSelectedItem.itemData.price));
        }
    }

    // =========================================================
    // PHẦN KẾT NỐI API 
    // =========================================================

    IEnumerator FetchUserData()
    {
        Debug.Log("-> Đang tải dữ liệu User từ Server...");

        string url = $"https://sentinels-shop.onrender.com/api/user/{currentUserId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);

                if (response.success)
                {
                    userCoin = response.data.coin;
                    txtCoin.text = userCoin.ToString();

                    userInventory.Clear();
                    foreach (var item in response.data.inventory)
                    {
                        userInventory.Add(item.itemId);
                    }

                    equippedBG = response.data.equippedBackgroundID;
                    equippedEffect = response.data.equippedEffectID;

                    // --- VỪA VÀO GAME LÀ ĐỔI ẢNH NỀN THEO DATABASE ---
                    ApplyBackgroundToMenu(equippedBG);

                    UpdateButtonState();
                    Debug.Log("Đã đồng bộ dữ liệu Shop thành công!");
                }
            }
            else
            {
                Debug.LogError("Không thể tải dữ liệu User: " + request.error);
            }
        }
    }

    IEnumerator BuyItemRoutine(string id, string category, int price)
    {
        Debug.Log("-> Đang gọi API Mua đồ thật...");

        string jsonRequest = $"{{\"userId\":\"{currentUserId}\", \"itemId\":\"{id}\", \"category\":\"{category}\", \"price\":{price}}}";

        // Lưu ý: Đảm bảo link này cùng chỗ với link trong FetchUserData (Render hoặc Localhost)
        string url = "https://sentinels-shop.onrender.com/api/buyItem";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Mua thành công! Server trả về: " + request.downloadHandler.text);

                var response = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);

                if (response.success)
                {
                    userCoin = response.data.coin;
                    txtCoin.text = userCoin.ToString();

                    userInventory.Add(id);
                    UpdateButtonState();
                }
            }
            else
            {
                Debug.LogError("Lỗi khi mua: " + request.error);
                Debug.LogError("Chi tiết: " + request.downloadHandler.text);
            }
        }
    }

    IEnumerator EquipItemRoutine(string id, string category)
    {
        Debug.Log("-> Đang gọi API Equip thật: " + id);

        string jsonRequest = $"{{\"userId\":\"{currentUserId}\", \"itemId\":\"{id}\", \"category\":\"{category}\"}}";
        string url = "https://sentinels-shop.onrender.com/api/equipItem";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Trang bị thành công! Server báo: " + request.downloadHandler.text);

                if (category == "Background")
                {
                    equippedBG = id;
                    // --- BẤM EQUIP LÀ ĐỔI ẢNH NỀN BÊN NGOÀI NGAY LẬP TỨC ---
                    ApplyBackgroundToMenu(id);
                }
                if (category == "Effect") equippedEffect = id;

                UpdateButtonState();
            }
            else
            {
                Debug.LogError("Lỗi khi Equip: " + request.error);
            }
        }
    }

    // --- HÀM THAY ĐỔI ẢNH NỀN ---
    private void ApplyBackgroundToMenu(string bgID)
    {
        if (realMenuBackground == null) return;

        foreach (var item in allItems)
        {
            if (item.itemId == bgID)
            {
                realMenuBackground.sprite = item.backgroundSprite;
                return;
            }
        }
    }

    private void OnBtnBuyCoinClick()
    {
        string shopUrl = _shopServer.GetShopUrl(currentUserId);
        Application.OpenURL(shopUrl);
    }

    private void OnBtnCloseClick()
    {
        _utils.HidePopup();
    }

    [System.Serializable]
    public class UserResponse { public bool success; public UserData data; }

    [System.Serializable]
    public class UserData
    {
        public int coin;
        public List<InventoryItem> inventory;
        public string equippedBackgroundID;
        public string equippedEffectID;
    }

    [System.Serializable]
    public class InventoryItem { public string itemId; }
}