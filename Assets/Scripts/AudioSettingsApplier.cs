using UnityEngine;
using UnityEngine.Audio;
using System.Collections; 

[RequireComponent(typeof(AudioSource))] 
public class AudioSettingsApplier : MonoBehaviour
{
    [Header("Cấu hình Mixer")]
    public AudioMixer audioMixer;

    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    // --- THÊM MỚI: CÔNG TẮC ĐỂ CHỌN PHÁT LUÔN HAY CHỜ ĐỢI ---
    [Header("Cài đặt Phát Nhạc")]
    public bool playOnStart = true; 

    [Header("Hiệu ứng Fade-In (To dần)")]
    public float fadeInDuration = 2.5f; 

    [Header("Cắt nhạc lặp liền mạch (Seamless Loop)")]
    public bool useCustomLoop = true;
    public float loopStartTime = 0f;
    public float loopEndTime = 60f; 

    private AudioSource _audioSource;
    
    // BIẾN THÊM VÀO ĐỂ GHI NHỚ MỨC ÂM LƯỢNG BẠN SET NGOÀI INSPECTOR
    private float _targetVolume; 

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // Ghi nhớ mức volume bạn chỉnh ở ngoài (VD: 0.135) trước khi nó bị ép về 0 để Fade-in
        _targetVolume = _audioSource.volume; 
        
        ApplyMixerSettings();

        // NẾU BẬT TỰ ĐỘNG PHÁT (NHƯ TẦNG 1) THÌ CHẠY LUÔN
        if (playOnStart)
        {
            StartCoroutine(FadeInMusicRoutine());
        }
    }

    // --- THÊM MỚI: HÀM NÀY ĐỂ TẦNG 2 GỌI KHI ĐẾM NGƯỢC XONG ---
    public void PlayAndFadeIn()
    {
        if (_audioSource != null) StartCoroutine(FadeInMusicRoutine());
    }

    void ApplyMixerSettings()
    {
        float masterVol = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float musicVol = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(masterVol, 0.0001f)) * 20);
            audioMixer.SetFloat("BGMVol", Mathf.Log10(Mathf.Max(musicVol, 0.0001f)) * 20);
            audioMixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(sfxVol, 0.0001f)) * 20);
        }
    }

    private IEnumerator FadeInMusicRoutine()
    {
        _audioSource.volume = 0f;
        if (useCustomLoop) _audioSource.loop = false; 
        
        _audioSource.Play();
        float currentTime = 0f;

        while (currentTime < fadeInDuration)
        {
            currentTime += Time.deltaTime;
            // SỬA CHỖ NÀY: Fade lên đúng cái số _targetVolume (0.135) chứ không ép lên 1 nữa
            _audioSource.volume = Mathf.Lerp(0f, _targetVolume, currentTime / fadeInDuration);
            yield return null;
        }
        
        // Chốt lại đúng mức âm lượng bạn muốn
        _audioSource.volume = _targetVolume;
    }

    void Update()
    {
        if (useCustomLoop && _audioSource.isPlaying)
        {
            if (_audioSource.time >= loopEndTime)
            {
                _audioSource.time = loopStartTime;
            }
        }
    }
}