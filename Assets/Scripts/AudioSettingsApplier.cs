using UnityEngine;
using UnityEngine.Audio;
using System.Collections; // Bắt buộc phải có thư viện này để chạy Coroutine (Fade-in)

[RequireComponent(typeof(AudioSource))] // Đảm bảo script này luôn đi kèm với AudioSource
public class AudioSettingsApplier : MonoBehaviour
{
    [Header("Cấu hình Mixer")]
    public AudioMixer audioMixer;

    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    [Header("Hiệu ứng Fade-In (To dần)")]
    [Tooltip("Thời gian để nhạc to từ 0 lên mức tối đa (tính bằng giây)")]
    public float fadeInDuration = 2.5f; 

    [Header("Cắt nhạc lặp liền mạch (Seamless Loop)")]
    public bool useCustomLoop = true;
    [Tooltip("Thời điểm lặp lại (giây). Thường là 0")]
    public float loopStartTime = 0f;
    [Tooltip("Thời điểm ngắt nhạc trước khi nó bị nhỏ dần (giây)")]
    public float loopEndTime = 60f; 

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        // 1. Áp dụng mức âm lượng từ Menu (ổ cứng) vào Mixer
        ApplyMixerSettings();

        // 2. Chạy hiệu ứng nhạc to dần
        StartCoroutine(FadeInMusicRoutine());
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
        // Ép âm lượng của AudioSource về 0 trước khi phát
        _audioSource.volume = 0f;
        
        // Tắt tính năng Loop mặc định của Unity (vì mình sẽ tự code Loop)
        if (useCustomLoop) _audioSource.loop = false; 
        
        _audioSource.Play();

        float currentTime = 0f;

        // Từ từ tăng Volume từ 0 lên 1 trong khoảng thời gian fadeInDuration
        while (currentTime < fadeInDuration)
        {
            currentTime += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(0f, 1f, currentTime / fadeInDuration);
            yield return null;
        }

        // Đảm bảo kết thúc hiệu ứng thì volume đạt mức 100% (của AudioSource)
        _audioSource.volume = 1f;
    }

    void Update()
    {
        // 3. LOGIC LẶP NHẠC LIỀN MẠCH
        if (useCustomLoop && _audioSource.isPlaying)
        {
            // Nếu thời gian bài hát chạm đến điểm "cắt" -> Ép nó tua lại về điểm bắt đầu
            if (_audioSource.time >= loopEndTime)
            {
                _audioSource.time = loopStartTime;
            }
        }
    }
}