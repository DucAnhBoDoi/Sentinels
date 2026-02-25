using UnityEngine;

[CreateAssetMenu(fileName = "ShopServerSO", menuName = "Scriptable Objects/ShopServerSO")]
public class ShopServerSO : ScriptableObject
{
    [SerializeField]
    private string _shopServerUrl;

    public string GetShopUrl(string userId)
    {
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrWhiteSpace(userId))
        {
            return $"{_shopServerUrl}/?quickLogin={userId}";
        }
        return $"{_shopServerUrl}/login";
    }

    public string GetLoginUrl()
    {
        return $"{_shopServerUrl}/api/login";
    }

    public string GetRegisterUrl()
    {
        return $"{_shopServerUrl}/api/register";
    }

    public string GetUserInfoUrl(string userId)
    {
        return $"{_shopServerUrl}/api/user/{userId}";
    }
}
