using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private MusicLibrary musicLibrary;

    [SerializeField] private Slider musicSlider;

    [SerializeField] private float baseVol = 0.3f;

    [Header("Optional Default Group")]
    [SerializeField] private string defaultMusicGroup;

    private float baseVolume = 1f;
    private float menuVolumeMultiplier = 0.5f;

    private float currentSliderValue = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            musicLibrary = GetComponent<MusicLibrary>();

        }
        if (!string.IsNullOrEmpty(defaultMusicGroup))
        {
            Play(defaultMusicGroup);
        }

    }
    public void SetMenuMusic(bool inMenu)
{
    if (inMenu)
    {
        baseVolume = audioSource.volume;
        audioSource.volume = baseVolume * menuVolumeMultiplier;
    }
    else
    {
        audioSource.volume = baseVolume;
    }
}
    void Start()
    {
        baseVolume = audioSource.volume;
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnSliderChanged);

            // initialize volume correctly
            OnSliderChanged(musicSlider.value);
        }

    }

    // 🎚 Handles slider changes (single source of truth)
    private void OnSliderChanged(float value)
    {
        currentSliderValue = value;
        ApplyVolume();
    }

    // 🎧 Applies final scaled volume
    private void ApplyVolume()
    {
        if (Instance == null || audioSource == null) return;

        audioSource.volume = currentSliderValue * baseVol;
    }

    // 🎵 MAIN PLAY FUNCTION (STATIC)
    public static void Play(string groupName, bool restart = true)
    {
        if (Instance == null || Instance.musicLibrary == null)
            return;

        AudioClip clip = Instance.musicLibrary.GetRandomClip(groupName);

        if (clip == null)
            return;

        if (restart)
        {
            Instance.audioSource.Stop();
        }
        
        Instance.audioSource.clip = clip;
        Instance.audioSource.Play();

        // ensure volume is correct when new clip starts
        Instance.ApplyVolume();
    }

    // ⏸ Pause music
    public static void Pause()
    {
        if (Instance != null)
            Instance.audioSource.Pause();
    }

    // ▶ Resume music
    public static void Resume()
    {
        if (Instance != null && Instance.audioSource != null)
            Instance.audioSource.UnPause();
    }

    // 🔊 Optional direct override (rarely needed now)
    public static void SetVolume(float volume)
    {
        if (Instance != null)
            Instance.audioSource.volume = volume;
    }

    internal void FadeOut()
    {
        throw new NotImplementedException();
    }
        public void FadeOut(float duration = 0.5f)
    {
        StartCoroutine(FadeMusic(0f, duration));
    }

    public void FadeIn(float targetVolume, float duration = 0.5f)
    {
        StartCoroutine(FadeMusic(targetVolume, duration));
    }

    private IEnumerator FadeMusic(float target, float duration)
    {
        float start = audioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        audioSource.volume = target;
    }
}