using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Needed for Button
using UnityEngine.EventSystems; // Needed to maintain keyboard focus

public class InventoryUI : MonoBehaviour
{
    public InventoryController inventoryController;
    public ItemDict itemDictionary;
    public GameObject uiItemPrefab;
    public Transform uiContentPanel;

    void OnEnable()
    {
        inventoryController = FindAnyObjectByType<InventoryController>();
        itemDictionary = FindAnyObjectByType<ItemDict>();

        if (inventoryController != null)
        {
            inventoryController.inventoryUI = this; 
            if (itemDictionary != null && uiContentPanel != null)
            {
                RefreshInventoryUI();
            }
        }
    }

    public void RefreshInventoryUI()
    {
        if (uiContentPanel == null || inventoryController == null || itemDictionary == null) return;

        // Deactivate existing rows so we don't have "ghost" items from a previous open
        foreach (Transform child in uiContentPanel)
        {
            child.gameObject.SetActive(false);
        }

        List<InventorySaveData> currentItems = inventoryController.GetInventoryItems();

        for (int i = 0; i < currentItems.Count; i++)
        {
            Item itemData = itemDictionary.GetItemData(currentItems[i].itemID);
            if (itemData == null) continue;

            GameObject itemRow;
            if (i < uiContentPanel.childCount)
            {
                itemRow = uiContentPanel.GetChild(i).gameObject;
                itemRow.SetActive(true);
            }
            else
            {
                itemRow = Instantiate(uiItemPrefab, uiContentPanel);
            }

            // --- FIXED TEXT UPDATING ---
            // We find by name so it doesn't matter what order they are in the hierarchy
            TextMeshProUGUI nameTxt = itemRow.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI quantTxt = itemRow.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descTxt = itemRow.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();

            if (nameTxt != null) nameTxt.text = itemData.itemName;
            if (quantTxt != null) quantTxt.text = "x" + currentItems[i].count.ToString();
            if (descTxt != null) descTxt.text = itemData.description; // This was being skipped[cite: 9]

            // --- BUTTON SETUP ---
            Button btn = itemRow.GetComponent<Button>();
            if (btn == null) btn = itemRow.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            int idToUse = currentItems[i].itemID;
            btn.onClick.AddListener(() => UseItemOnPlayer(idToUse));
        }
    }

    // --- NEW: The logic for actually using the item ---
    public void UseItemOnPlayer(int itemID)
{
    Item itemData = itemDictionary.GetItemData(itemID);
    if (itemData == null) return;

    if (PartyManager.Instance != null && PartyManager.Instance.partyMembers.Count > 0)
    {
        Unit mainPlayer = PartyManager.Instance.partyMembers[0];

        // 1. APPLY EFFECTS (Modify these to match your Item class variables)
        if (itemData.effect == 'h') // Example: if 'h' means healing
        {
            mainPlayer.currentHP += itemData.potency;
            if (mainPlayer.currentHP > mainPlayer.maxHP) mainPlayer.currentHP = mainPlayer.maxHP;
        }

        Debug.Log($"Used {itemData.itemName} on {mainPlayer.unitName}.");

        // 2. REMOVE THE ITEM FROM INVENTORY
        inventoryController.RemoveItem(itemID, 1); 

        // 3. REFRESH UI
        RefreshInventoryUI();

        // 4. MAINTAIN KEYBOARD FOCUS
        StartCoroutine(ReselectFirstItem());
    }
}

    // Helps maintain keyboard navigation after the UI re-draws itself
    private IEnumerator ReselectFirstItem()
    {
        yield return new WaitForEndOfFrame();
        
        // Find the first active item row and focus it
        foreach (Transform child in uiContentPanel)
        {
            if (child.gameObject.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(child.gameObject);
                break;
            }
        }
    }
}