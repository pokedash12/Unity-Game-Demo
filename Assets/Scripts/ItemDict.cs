using System.Collections.Generic;
using UnityEngine;

public class ItemDict : MonoBehaviour
{
    // Drag your .asset files (ScriptableObjects) into this list in the inspector
    public List<Item> itemDataAssets; 

    private Dictionary<int, Item> itemDictionary;

    private void Awake()
    {
        itemDictionary = new Dictionary<int, Item>();

        foreach (Item item in itemDataAssets)
        {
            if (item != null)
            {
                // Uses the ID defined inside the ScriptableObject
                itemDictionary[item.ID] = item;
            }
        }
    }

    public Item GetItemData(int itemID)
    {
        if (itemDictionary.TryGetValue(itemID, out Item data))
        {
            return data;
        }
        
        Debug.LogWarning($"Item ID {itemID} not found in Dictionary!");
        return null;
    }
}