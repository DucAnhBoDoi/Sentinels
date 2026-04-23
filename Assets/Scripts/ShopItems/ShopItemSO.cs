using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Item", menuName = "Scriptable Objects/ShopItemSO")]
public class ShopItemSO : ScriptableObject
{
    [Header("Item Info")]
    public string itemId;          // MÃ SỐ (VD: "BG_1")
    public string category;        // Phân loại ("Background" hoặc "Effect")
    public int price;              // Giá tiền (VD: 200)

    [Header("Display")]
    public Sprite icon;            
    public Sprite backgroundSprite; 
}