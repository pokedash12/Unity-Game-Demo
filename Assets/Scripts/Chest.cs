using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool IsOpened { get; private set; }
    public string chestID 
    {
        get 
        {
            // If the ID hasn't been made yet, make it the second someone asks for it
            if (string.IsNullOrEmpty(_chestID))
            {
                _chestID = GlobalHelper.GenerateUniqueID(gameObject);
            }
            return _chestID;
        }
    }
    private string _chestID;
    
    // Reference the Data Asset instead of a physical prefab
    public Item itemToGive; 
    
    public int amountToGive = 1;
    public Sprite openedSprite;
    
    void Start()
    {
    }

    // Update is called once per frame
    public bool CanInteract()
    {
        return !IsOpened;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        OpenChest();
    }


    public void OpenChest()
    {
        if (IsOpened || itemToGive == null) return;

        SetOpened(true);
        SoundEffectManager.Play("Chest");

        // Directly add the item to the inventory via its ID
        InventoryController inventory = FindAnyObjectByType<InventoryController>();
        if (inventory != null)
        {
            inventory.AddItem(itemToGive.ID, amountToGive);
            Debug.Log($"Gave {amountToGive} of {itemToGive.itemName} directly to inventory.");
        }
    }

    public void SetOpened(bool opened)
    {
        IsOpened = opened;
        if (opened && openedSprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
        }
    }
}
