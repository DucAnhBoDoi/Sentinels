using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MenuPopupOptionsController : MonoBehaviour
{
    [SerializeField]
    private MenuSceneUtils _utils;

    // THÊM: Ô để kéo thả file Audio Mixer vào
    [SerializeField]
    private AudioMixer _audioMixer;

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

    // THÊM: Các từ khóa để lưu vào bộ nhớ máy (PlayerPrefs)
    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Start()
    {
        // 1. Tải dữ liệu cũ đã lưu (Nếu chưa lưu bao giờ thì mặc định là 1)
        float masterVol = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float musicVol = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        // 2. Gắn giá trị cũ vào Slider để hiển thị
        _masterVolumeSlider.value = masterVol;
        _musicVolumeSlider.value = musicVol;
        _sfxVolumeSlider.value = sfxVol;

        // ---------------------------------------------------------
        SetMasterVolume(masterVol);
        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
        // ---------------------------------------------------------

        // 3. Lắng nghe sự kiện nút bấm (Của bạn giữ nguyên)
        _btnSave.onClick.AddListener(OnBtnSaveClick);
        _btnBack.onClick.AddListener(OnBtnBackClick);

        // 4. Lắng nghe thao tác kéo Slider để chỉnh âm thanh Real-time
        _masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        _musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        _sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    // --- THÊM: CÁC HÀM XỬ LÝ TOÁN HỌC ĐỂ CHUYỂN SLIDER SANG DECIBEL ---
    private void SetMasterVolume(float value)
    {
        _audioMixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }

    private void SetMusicVolume(float value)
    {
        _audioMixer.SetFloat("BGMVol", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }

    private void SetSFXVolume(float value)
    {
        _audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20);
    }

    private void OnBtnSaveClick()
    {
        // THÊM: Lưu thông số vào ổ cứng trước khi đóng cửa sổ
        PlayerPrefs.SetFloat(MASTER_KEY, _masterVolumeSlider.value);
        PlayerPrefs.SetFloat(MUSIC_KEY, _musicVolumeSlider.value);
        PlayerPrefs.SetFloat(SFX_KEY, _sfxVolumeSlider.value);
        PlayerPrefs.Save();

        Debug.Log("<color=green>[Options] Đã lưu thiết lập âm thanh thành công!</color>");

        _utils.HidePopup();
    }

    private void OnBtnBackClick()
    {
        _utils.HidePopup();
    }
}