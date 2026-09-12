using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class SaveData
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector3 playerPosition;
    public string mapBoundary;

    public List<InventorySaveData> inventorySaveData;
    public List<ChestSaveData> chestSaveData;
    public List<PlayerSaveData> partyData;
}

[System.Serializable]

public class ChestSaveData
{
    public string chestID;
    public bool isOpened;
}

[System.Serializable]
public class PlayerSaveData
{
    public string unitName;
    public int unitLevel;
    public int maxHP;
    public int maxMP;
    public int currentHP;
    public int currentMP;

    // --- NEW STATS ---
    public int strength;
    public int defense;
    public int magic;
    public int speed;
    public int luck;
    public int statPointsAvailable;
    // -----------------

    public List<string> activeSkills; 
    public List<string> allSkills;
    public char[] weaknesses;
    public char[] resistances;
}

