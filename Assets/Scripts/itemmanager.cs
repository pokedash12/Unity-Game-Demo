using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    // TRACKERS
    private HashSet<string> openedChests = new HashSet<string>();
    public Vector3 lastOverworldPosition;
    public bool hasStoredPosition = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);    
        }
    }

    // Methods to manage chest states
    public void RegisterChestOpened(string chestID) => openedChests.Add(chestID);
    public bool IsChestOpened(string chestID) => openedChests.Contains(chestID);
}