using UnityEngine;
using UnityEngine.UI;

public class PopupRoomController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private Button _btnClose; 

    private void Start()
    {
        _btnClose.onClick.AddListener(() => _utils.HidePopup());
    }
}