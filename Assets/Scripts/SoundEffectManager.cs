using UnityEngine;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    private static SoundEffectManager Instance;

    private static AudioSource audioSource;

    private static AudioSource instance;
    private static AudioSource randomPitchAudioSource;
    private static AudioSource voiceAudioSource;
    private static SoundEffectLibrary soundEffectLibrary;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if(Instance == null)
        {
        Instance = this;
        AudioSource[] audioSources = GetComponents<AudioSource>();
        audioSource = audioSources[0];
        randomPitchAudioSource = audioSources[1];
        voiceAudioSource = audioSources[2];
        soundEffectLibrary = GetComponent<SoundEffectLibrary>();
        }

    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        AudioClip audioClip = soundEffectLibrary.GetRandomClip(soundName);
        if(audioClip != null)
        {
            if (randomPitch)
            {
                randomPitchAudioSource.pitch = Random.Range(1f,1.5f);

                randomPitchAudioSource.PlayOneShot(audioClip);
            }
            else
            {
                audioSource.PlayOneShot(audioClip);
            }
        }
    }
    public static AudioClip GetClip(string name)
    {
        // Check if the library exists and the instance is initialized
        if (Instance == null || soundEffectLibrary == null)
        {
            Debug.LogWarning("SoundEffectManager: Instance or Library is missing!");
            return null;
        }

        // Use the library to find and return a clip by its group name
        return soundEffectLibrary.GetRandomClip(name);
    }

    public static void PlayVoice(AudioClip audioClip, float volumeScale = 1f)
    {
        voiceAudioSource.PlayOneShot(audioClip);
    }


    public static void SetVolume(float volume)
    {
        audioSource.volume = volume;
        randomPitchAudioSource.volume = volume;
        voiceAudioSource.volume = volume;
    }

    public void OnValueChanged()
    {
        SetVolume(sfxSlider.value);
    }

    void Start()
    {
        sfxSlider.onValueChanged.AddListener(delegate { OnValueChanged();});
    }
}
