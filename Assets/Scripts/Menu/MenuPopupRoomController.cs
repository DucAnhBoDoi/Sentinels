using UnityEngine;
using UnityEngine.UI;

public class PopupRoomController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [Header("Buttons")]
    [SerializeField]
    private Button _btnClose;

    [SerializeField]
    private Button _btnTabOnline; 

    [SerializeField]
    private Button _btnTabLocal; 
    
    [SerializeField]
    private Button _btnLoad;      

    [Header("Content Containers")]
    [SerializeField]
    private GameObject _contentOnline; 

    [SerializeField]
    private GameObject _contentLocal;

    private bool _isOnlineMode = true;

    private void Start()
    {
        _btnClose.onClick.AddListener(() => _utils.HidePopup());

        _btnTabOnline.onClick.AddListener(OnBtnTabOnlineClick);
        _btnTabLocal.onClick.AddListener(OnBtnTabLocalClick);

        if (_btnLoad != null)
        {
            _btnLoad.onClick.AddListener(OnBtnLoadClick);
        }

        OnBtnTabOnlineClick();
    }

    private void OnBtnTabOnlineClick()
    {
        _isOnlineMode = true;

        _contentOnline.SetActive(true);
        _contentLocal.SetActive(false);

        _btnTabOnline.interactable = false; 
        _btnTabLocal.interactable = true;

        Debug.Log("Đã chuyển sang Tab ONLINE");
        // TODO: Sau này sẽ gọi hàm tìm phòng Online ở đây
        // LobbyManager.Instance.RefreshList();
    }

    private void OnBtnTabLocalClick()
    {
        _isOnlineMode = false;

        _contentOnline.SetActive(false);
        _contentLocal.SetActive(true);

        _btnTabOnline.interactable = true;
        _btnTabLocal.interactable = false;

        Debug.Log("Đã chuyển sang Tab LOCAL");
        // TODO: Sau này sẽ gọi hàm tìm phòng LAN ở đây
        // NetworkDiscovery.Search();
    }

    private void OnBtnLoadClick()
    {
        if (_isOnlineMode)
        {
            Debug.Log("Đang tải lại danh sách Online...");
        }
        else
        {
            Debug.Log("Đang quét lại mạng LAN...");
        }
    }
}