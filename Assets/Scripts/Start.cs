using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.IO;

public class MainMenu : MonoBehaviour
{
    public GameObject firstButton;

    void Start()
    {
        // Force the first button to be selected so keyboard/controller works immediately
        if (firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Overworld");
    }

    public void ClearSaveData()
    {
        // Define the path to your save file (matching SaveController logic)
        string path = Application.persistentDataPath + "/saveData.json";

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save data cleared!");
        }
        else
        {
            Debug.Log("No save data found to delete.");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}