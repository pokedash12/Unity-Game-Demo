using UnityEngine;

public class UISFXManager : MonoBehaviour
{
    public static UISFXManager Instance;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Audio Clips")]
    public AudioClip navSound;
    public AudioClip selectSound;
    public AudioClip backSound;

    private void Awake()
    {
        // Ensure only one of these exists and it survives scene loads
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayNav()
    {
        if (navSound != null && audioSource != null) 
            audioSource.PlayOneShot(navSound);
    }

    public void PlaySelect()
    {
        if (selectSound != null && audioSource != null) 
            audioSource.PlayOneShot(selectSound);
    }

    public void PlayBack()
    {
        if (backSound != null && audioSource != null) 
            audioSource.PlayOneShot(backSound);
    }
}