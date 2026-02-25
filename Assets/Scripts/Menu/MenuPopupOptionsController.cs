using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MenuPopupOptionsController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    [SerializeField]
    private Button _btnSave;

    [SerializeField]
    private Button _btnBack;

    [SerializeField]
    private Slider _masterVolumeSlider;

    [SerializeField]
    private Slider _musicVolumeSlider;

    [SerializeField]
    private Slider _sfxVolumeSlider;

    private void Start()
    {
        _btnSave.onClick.AddListener(OnBtnSaveClick);
        _btnBack.onClick.AddListener(OnBtnBackClick);
    }

    private void OnBtnSaveClick()
    {
        _utils.HidePopup();
    }

    private void OnBtnBackClick()
    {
        _utils.HidePopup();
    }

}
