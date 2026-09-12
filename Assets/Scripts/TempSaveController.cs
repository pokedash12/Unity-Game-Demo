using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TempSaveController : MonoBehaviour
{
    public string tempPath;
    private InventoryController inventory;

    void Awake()
    {
        tempPath = Path.Combine(Application.persistentDataPath, "tempSave.json");
        inventory = FindAnyObjectByType<InventoryController>();
    }

    [System.Obsolete]
    void Start()
    {
        // When the Overworld loads, check for a temp save
        if (File.Exists(tempPath))
        {
            LoadTempState();
        }
    }

    [System.Obsolete]
    public void CreateTempSave()
    {
        InventoryController inventory = FindAnyObjectByType<InventoryController>();
        
        // THE FIX: Safely find the player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 pos = Vector3.zero;

        if (player != null)
        {
            pos = player.transform.position;
        }
        else
        {
            Debug.LogWarning("TempSave: Player not found! Saving position as Zero.");
        }
        
        SaveData tempDetails = new SaveData
        {
            playerPosition = pos,
            // Capture all chests currently in this scene
            chestSaveData = GetCurrentSceneChestStates(),
            inventorySaveData = inventory != null ? inventory.GetInventoryItems() : new List<InventorySaveData>()
        };

        string json = JsonUtility.ToJson(tempDetails);
        File.WriteAllText(tempPath, json);
        Debug.Log("Temp state cached successfully.");
    }

    [System.Obsolete]
    private void LoadTempState()
    {
        string json = File.ReadAllText(tempPath);
        SaveData tempDetails = JsonUtility.FromJson<SaveData>(json);

        // 1. Restore Player Position
        GameObject.FindGameObjectWithTag("Player").transform.position = tempDetails.playerPosition;

        // 2. Restore Chests
        Chest[] sceneChests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach (Chest chest in sceneChests)
        {
            var savedChest = tempDetails.chestSaveData.Find(c => c.chestID == chest.chestID);
            if (savedChest != null)
            {
                chest.SetOpened(savedChest.isOpened);
            }
        }

        // 3. Cleanup: Delete the temp file so it doesn't load again next time
        File.Delete(tempPath);
        Debug.Log("Temporary state restored and file deleted.");
    }

    [System.Obsolete]
    private List<ChestSaveData> GetCurrentSceneChestStates()
    {
        List<ChestSaveData> states = new List<ChestSaveData>();
        Chest[] sceneChests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach (Chest chest in sceneChests)
        {
            states.Add(new ChestSaveData { chestID = chest.chestID, isOpened = chest.IsOpened });
        }
        return states;
    }
}