using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    // Tracks ItemID -> Quantity
    private Dictionary<int, int> inventoryCounts = new Dictionary<int, int>();

    public InventoryUI inventoryUI;

    // Add an item to the inventory
    public void AddItem(int itemID, int amount = 1)
    {
        if (inventoryCounts.ContainsKey(itemID))
        {
            inventoryCounts[itemID] += amount;
        }
        else
        {
            inventoryCounts[itemID] = amount;
        }
        Debug.Log($"Added {amount} of item {itemID}. Total: {inventoryCounts[itemID]}");
        UpdateUI();
    }

    public void RemoveItem(int itemID, int amount = 1)
    {
        if (inventoryCounts.ContainsKey(itemID))
        {
            inventoryCounts[itemID] -= amount;
            
            if (inventoryCounts[itemID] <= 0)
            {
                inventoryCounts.Remove(itemID);
            }
        }
        UpdateUI();
    }

    // Check how many of a specific item the player has
    public int GetItemCount(int itemID)
    {
        return inventoryCounts.TryGetValue(itemID, out int count) ? count : 0;
    }


    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> saveData = new List<InventorySaveData>();
        
        foreach (var kvp in inventoryCounts)
        {
            saveData.Add(new InventorySaveData { itemID = kvp.Key, count = kvp.Value });
        }
        
        return saveData;
    }

    private void UpdateUI()
    {
        // Note: If your UI Canvas is turned off/disabled, FindAnyObjectByType might not find it 
        // unless you pass 'FindObjectsInactive.Include'. 
        InventoryUI ui = FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
    }

    public void SetInventoryItems(List<InventorySaveData> loadedData)
    {
        inventoryCounts.Clear();
        
        if (loadedData != null)
        {
            foreach (var data in loadedData)
            {
                inventoryCounts[data.itemID] = data.count;
            }
            UpdateUI();
        }
    }
    private void Awake()
    {
        // If you have multiple objects with this script, this ensures only one survives
        InventoryController[] controllers = FindObjectsByType<InventoryController>(FindObjectsInactive.Include);
        if (controllers.Length > 1)
        {
            foreach (var controller in controllers)
            {
                if (controller != this && controller.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    Destroy(controller.gameObject);
                }
            }
        }
    }
        
}