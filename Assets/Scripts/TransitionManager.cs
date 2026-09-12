using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void OnEnable()
    {
        // Subscribe to the sceneLoaded event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Find the new canvas in the current scene
        GameObject canvasObj = GameObject.Find("FadePanel W"); 
        if (canvasObj != null)
        {
            fadeCanvas = canvasObj.GetComponent<CanvasGroup>();
        }
    }

    [System.Obsolete]
    public void LoadScene(string sceneName)
    {
        StartCoroutine(Transition(sceneName));
    }

    [System.Obsolete]
    IEnumerator Transition(string sceneName)
    {
        yield return StartCoroutine(Fade(1)); // fade out
        if (sceneName.Equals("Battle"))
        {
            SaveController saveController = FindObjectOfType<SaveController>();
            if (saveController != null)
            {
                
                saveController.TempSaveGame();
                Debug.LogWarning("SABWS");
            }
            else
            {
                Debug.LogWarning("SaveController instance not found before loading Battle scene.");
            }
        }

        yield return SceneManager.LoadSceneAsync(sceneName);
        if (sceneName.Equals("Overworld"))
        {
            SaveController saveController = FindObjectOfType<SaveController>();
            if (saveController != null)
            {
                saveController.LoadGame();
                Debug.LogWarning("SABWS AGAIN");
            }
            else
            {
                Debug.LogWarning("SaveController instance not found before loading Battle scene.");
            }
            PauseController.SetPause(false);
        }
        
        yield return StartCoroutine(Fade(0)); // fade in
    }

    IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvas.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
    }
}