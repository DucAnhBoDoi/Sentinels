using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))] // Dòng này ép Unity TỰ ĐỘNG THÊM Button nếu anh lỡ quên!
public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image imageItem;       
    public TextMeshProUGUI txtCost; 
    private Button btnItem;       

    [HideInInspector] 
    public ShopItemSO itemData;   
    
    private MenuPopupShopController shopController;

    private void Awake()
    {
        btnItem = GetComponent<Button>();
    }

    // Hàm này sẽ được ShopController gọi để đổ dữ liệu vào ô
    public void Setup(ShopItemSO data, MenuPopupShopController controller)
    {
        itemData = data;
        shopController = controller;

        // --- CHỐT CHẶN AN TOÀN ---
        if (btnItem == null) btnItem = GetComponent<Button>(); 
        
        if (btnItem == null)
        {
            Debug.LogError(" BÁO ĐỘNG: " + gameObject.name + " bị thiếu component Button! Hãy Add Component Button cho nó.");
            return; // Dừng lại để không bị văng lỗi màn hình
        }

        if (imageItem == null || txtCost == null)
        {
            Debug.LogError(" BÁO ĐỘNG: " + gameObject.name + " chưa được kéo thả Image hoặc TxtCost vào Inspector!");
            return;
        }
        // -------------------------

        // Cập nhật hình ảnh và giá tiền
        imageItem.sprite = data.icon;
        txtCost.text = data.price.ToString();

        // Gắn sự kiện click
        btnItem.onClick.RemoveAllListeners();
        btnItem.onClick.AddListener(() => shopController.OnItemSelected(this));
    }
}