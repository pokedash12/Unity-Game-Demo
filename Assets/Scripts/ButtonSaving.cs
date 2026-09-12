using UnityEngine;

public class SaveButtonHelper : MonoBehaviour
{
    public void TriggerSave()
    {
        // Find the persistent controller and tell it to save
        SaveController controller = FindAnyObjectByType<SaveController>();
        if (controller != null)
        {
            controller.SaveGame();
            Debug.Log("Save triggered via Helper.");
        }
    }
}