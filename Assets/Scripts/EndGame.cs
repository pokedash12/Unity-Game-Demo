using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGame : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object entering the trigger is the player
        if (collision.CompareTag("Player"))
        {
            LoadCredits();
        }
    }

    private void LoadCredits()
    {
        Debug.Log("End Game Triggered. Loading Credits...");

        // If you have a SceneTransitionManager (like in your Main Menu), use it for a fade-out
        SceneTransitionManager transition = FindAnyObjectByType<SceneTransitionManager>();

        if (transition != null)
        {
            transition.LoadScene("Credits");
        }
        else
        {
            // Fallback to direct loading if no transition manager is found
            SceneManager.LoadSceneAsync("Credits");
        }
    }
}