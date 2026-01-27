using UnityEngine;
using UnityEngine.UI;

public class MainMenuPopupOptionsController : MonoBehaviour
{
    [SerializeField]
    private Button _btnSave;

    [SerializeField]
    private Button _btnBack;

    [SerializeField]
    private GameObject _popupOptions;

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
        _popupOptions.SetActive(false);
    }

    private void OnBtnBackClick()
    {
        _popupOptions.SetActive(false);
    }

}
