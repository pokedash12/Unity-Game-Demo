using System;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    [SerializeField] CanvasGroup canvasGroup;

    [SerializeField] float fadeDuration = 0.5f;
    [SerializeField] CinemachineCamera vcam;

    CinemachinePositionComposer composer;
    Vector3 originalDamping;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        composer = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body) 
                as CinemachinePositionComposer;
        originalDamping = composer.Damping;
    
    }

    async Task Fade(float targetTransparency)
    {
        float start = canvasGroup.alpha, t = 0;
        while(t < fadeDuration)
        {
            t+= Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetTransparency, t/fadeDuration);
            await Task.Yield();
        }
        canvasGroup.alpha = targetTransparency;
    }

    public async Task FadeOut()
    {
        await Fade(1);
        SetDamping(Vector3.zero);
    }
    public async Task FadeIn()
    {
        await Fade(0);
        SetDamping(originalDamping);
    }

    void SetDamping(Vector3 d)
    {
        if (!composer) return;
        composer.Damping = new Vector3(d.x, d.y, d.z);
    }
}
