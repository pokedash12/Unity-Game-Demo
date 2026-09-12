using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class SaveController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private string saveLocation;
    private InventoryController inventoryController;
    private Chest[] chests;

    public string tempPath;
    public GameObject playerPrefab;

    public string tempPath2;
    public SaveData tempDetails;
    public SaveData tempDetails2;

    public List<Skill> masterSkillDatabase;

    public Unit playerInfo;

    public GameObject justPlayer;
    public List<PlayerSaveData> allPartyData;

    void Start()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        inventoryController = FindAnyObjectByType<InventoryController>();
        chests = FindObjectsByType<Chest>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerInfo = playerObj.GetComponent<Unit>();
        LoadGame();
    }
    private List<PlayerSaveData> PackPartyData()
    {
        List<PlayerSaveData> allPartyData = new List<PlayerSaveData>();
        
        foreach (Unit member in PartyManager.Instance.partyMembers)
        {
            allPartyData.Add(new PlayerSaveData 
            {
                unitName = member.unitName,
                unitLevel = member.unitLevel,
                maxHP = member.maxHP,
                maxMP = member.maxMP,
                currentHP = member.currentHP,
                currentMP = member.currentMP,

                // --- SAVE NEW STATS ---
                strength = member.strength,
                defense = member.defense,
                magic = member.magic,
                speed = member.speed,
                luck = member.luck,
                statPointsAvailable = member.statPointsAvailable,
                // ----------------------

                activeSkills = member.skills?.Where(s => s != null).Select(s => s.skillName).ToList() ?? new List<string>(),
                allSkills = member.allSkills?.Where(s => s != null).Select(s => s.skillName).ToList() ?? new List<string>(),
                weaknesses = member.weaknesses,
                resistances = member.resistances,
            });
        }
        return allPartyData;
    }

    // Update is called once per frame
    public void SaveGame()
    {
        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position,
            mapBoundary = FindAnyObjectByType<CinemachineConfiner2D>().BoundingShape2D.gameObject.name,
            inventorySaveData = inventoryController.GetInventoryItems(),
            chestSaveData = GetChestsState(),
            partyData = PackPartyData()
        };
        print("THE GAME HAS BEEN SAVED");

        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
    }

    public void TempSaveGame()
    {
        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position,
            mapBoundary = FindAnyObjectByType<CinemachineConfiner2D>().BoundingShape2D.gameObject.name,
            inventorySaveData = inventoryController.GetInventoryItems(),
            chestSaveData = GetChestsState(),
            partyData = PackPartyData()
        };
        string folderPath = Path.Combine(Application.persistentDataPath, "bruh");
        string filePath = Path.Combine(folderPath, "tempSaveData.json");
        tempPath = filePath;

        // Check if the "bruh2" folder exists
        if (!Directory.Exists(folderPath))
        {
            // If not, build it!
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllText(filePath, JsonUtility.ToJson(saveData));
        Debug.LogWarning("TEMP DATA MADE");
    }

    public void TempSaveGame2()
    {
        SaveData saveData = new SaveData
        {
            partyData = allPartyData
        };
        string folderPath = Path.Combine(Application.persistentDataPath, "bruh2");
        string filePath = Path.Combine(folderPath, "tempSaveData2.json");
        tempPath2 = filePath;

        // Check if the "bruh2" folder exists
        if (!Directory.Exists(folderPath))
        {
            // If not, build it!
            Directory.CreateDirectory(folderPath);
        }
        File.WriteAllText(filePath, JsonUtility.ToJson(saveData));
        Debug.LogWarning("TEMP DATA MADE");
    }


    private List<ChestSaveData> GetChestsState()
    {
        List<ChestSaveData> chestStates = new List<ChestSaveData>();

        foreach(Chest chest in chests)
        {
            ChestSaveData chestSaveData = new ChestSaveData
            {
                chestID = chest.chestID,
                isOpened = chest.IsOpened
            };
            chestStates.Add(chestSaveData);
        }

        return chestStates;
    }
    public void LoadGame()
    {   
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        justPlayer = GameObject.FindGameObjectWithTag("Player");

        // 1. If the player isn't in the scene, spawn the prefab
        if (playerObj == null)
        {
            print("NOT IN THE SCENE");
            playerObj = Instantiate(playerPrefab);
            playerInfo = playerObj.GetComponent<Unit>();
        }
        else
        {
            print("NOT IN THE SCENE");
            playerInfo = playerObj.GetComponent<Unit>();
        }
        print("Checking for load game");
        if (File.Exists(saveLocation) && !File.Exists(tempPath))
        {
            print("THE SAVE HATH BEEN LOAEDED");
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));
            GameObject.FindGameObjectWithTag("Player").transform.position = saveData.playerPosition;
            
            FindAnyObjectByType<CinemachineConfiner2D>().BoundingShape2D = GameObject.Find(saveData.mapBoundary).GetComponent<PolygonCollider2D>();
            inventoryController.SetInventoryItems(saveData.inventorySaveData);
            LoadChestStates(saveData.chestSaveData);

            // Execute the plan: Load the data back into the player component
            for (int i = 0; i < saveData.partyData.Count; i++)
            {
                var data = saveData.partyData[i];

                // If the PartyManager doesn't have enough units in its list yet, 
                // you may need to instantiate them or grab them from a pool.
                if (i < PartyManager.Instance.partyMembers.Count)
                {
                    PartyManager.Instance.partyMembers[i].LoadPlayer(
                        data.unitName,
                        data.unitLevel,
                        data.maxHP,
                        data.maxMP,
                        data.currentHP,
                        data.currentMP,
                        data.strength,
                        data.defense,
                        data.magic,
                        data.speed,
                        data.luck,
                        data.statPointsAvailable,
                        GetSkillsFromNames(data.activeSkills),
                        GetSkillsFromNames(data.allSkills),
                        data.weaknesses,
                        data.resistances
                    );
                }
                else 
                {
                    Debug.LogWarning($"Save data found for {data.unitName}, but no Unit slot exists in PartyManager at index {i}!");
                }
            }
            
        }
        else if (File.Exists(tempPath))
        {
            tempDetails = JsonUtility.FromJson<SaveData>(File.ReadAllText(tempPath));
            tempDetails2 = JsonUtility.FromJson<SaveData>(File.ReadAllText(tempPath2));
            MusicManager mm = FindAnyObjectByType<MusicManager>();

            LoadChestStates(tempDetails.chestSaveData);

            // Execute the plan for Temp Data
            for (int i = 0; i < tempDetails2.partyData.Count; i++)
            {
                var data = tempDetails2.partyData[i];

                // If the PartyManager doesn't have enough units in its list yet, 
                // you may need to instantiate them or grab them from a pool.
                if (i < PartyManager.Instance.partyMembers.Count)
                {
                    PartyManager.Instance.partyMembers[i].LoadPlayer(
                        data.unitName,
                        data.unitLevel,
                        data.maxHP,
                        data.maxMP,
                        data.currentHP,
                        data.currentMP,
                        data.strength,
                        data.defense,
                        data.magic,
                        data.speed,
                        data.luck,
                        data.statPointsAvailable,
                        GetSkillsFromNames(data.activeSkills),
                        GetSkillsFromNames(data.allSkills),
                        data.weaknesses,
                        data.resistances
                    );
                }
                else 
                {
                    Debug.LogWarning($"Save data found for {data.unitName}, but no Unit slot exists in PartyManager at index {i}!");
                }
            }
            GameObject.FindGameObjectWithTag("Player").transform.position = tempDetails.playerPosition;
            FindAnyObjectByType<CinemachineConfiner2D>().BoundingShape2D = GameObject.Find(tempDetails.mapBoundary).GetComponent<PolygonCollider2D>();
            
            File.Delete(tempPath);
            Debug.Log("Temporary state restored and file deleted.");
        }
        else
        {
            SaveData dataToLoad = null;
            print("NO SAVE FOUND: LOADING DEFAULT TEMPLATE");
            TextAsset defaultFile = Resources.Load<TextAsset>("defaultStats");
            if (defaultFile != null)
            {
                dataToLoad = JsonUtility.FromJson<SaveData>(defaultFile.text);
                ApplyLoadedData(dataToLoad);
            }
            else
            {
                Debug.LogError("CRITICAL: Resources/defaultStats.json not found! Using hardcoded fallback.");
                return;
            }
        }

    DelayedMusicUpdate();
    }

    private void ApplyLoadedData(SaveData data)
    {
        // Position and World State
        GameObject.FindGameObjectWithTag("Player").transform.position = data.playerPosition;
        
        // Confiner Check: Only apply if boundary is set
        if (!string.IsNullOrEmpty(data.mapBoundary))
        {
            var boundaryObj = GameObject.Find(data.mapBoundary);
            if(boundaryObj != null)
                FindAnyObjectByType<CinemachineConfiner2D>().BoundingShape2D = boundaryObj.GetComponent<PolygonCollider2D>();
        }

        // Inventory and Chests
        inventoryController.SetInventoryItems(data.inventorySaveData);
        LoadChestStates(data.chestSaveData);

        // Party Stats (If not already handled by Temp logic)
        if (data.partyData != null && data.partyData.Count > 0)
        {
            ApplyPartyData(data.partyData);
        }
    }

    private void ApplyPartyData(List<PlayerSaveData> partyList)
    {
        for (int i = 0; i < partyList.Count; i++)
        {
            if (i < PartyManager.Instance.partyMembers.Count)
            {
                var pData = partyList[i];
                PartyManager.Instance.partyMembers[i].LoadPlayer(
                    pData.unitName, pData.unitLevel, pData.maxHP, pData.maxMP,
                    pData.currentHP, pData.currentMP, pData.strength, pData.defense,
                    pData.magic, pData.speed, pData.luck, pData.statPointsAvailable,
                    GetSkillsFromNames(pData.activeSkills),
                    GetSkillsFromNames(pData.allSkills),
                    pData.weaknesses, pData.resistances
                );
            }
        }
    }
    private void DelayedMusicUpdate()
        { // wait 1 frame

            if (justPlayer == null || MusicManager.Instance == null)
        {
            Debug.LogError("playerInfo is NULL in ForceMusicUpdate");
                return;
        }

            Collider2D hit = Physics2D.OverlapPoint(justPlayer.transform.position);

            if (hit != null)
            {
                print("THERE WAS A HIT");
                Debug.Log("Hit object: " + hit.gameObject.name);
            Debug.Log("Tag: " + hit.tag);
            Debug.Log("Layer: " + LayerMask.LayerToName(hit.gameObject.layer));
                EncounterZone zone = hit.GetComponent<EncounterZone>();
                print(zone.areaName);
                print(zone.areaMusic);
                print(zone.areaMusic);
                print(zone.areaMusic);
                print(zone.areaMusic);

                if (zone != null && !string.IsNullOrEmpty(zone.areaMusic))
                {
                    MusicManager.Play(zone.areaMusic, true);
                }
            }
        }
    private void InitializeNewPlayer()
    {
        Debug.Log("No save found. Initializing New Game stats.");
        
        // Define your starting values here
        playerInfo.LoadPlayer(
            "Hero",         // Name
            1,              // Level
            300,            // Max HP
            30,             // Max MP
            300,            // Current HP
            30,             // Current MP
            12,
            10,
            4,
            8,
            10,
            0,             // Damage
            new Skill[0],   // Starting active skills
            new Skill[0],   // Starting all skills
            new char[] { 'f' }, // Weaknesses (e.g., 'f' for fire)
            new char[0]     // Resistances
        );

        // Optionally, give them a starting skill from your master database
        if (masterSkillDatabase.Count > 0)
        {
            playerInfo.addSkill(masterSkillDatabase[2]); 
        }

    }
    private void LoadChestStates(List<ChestSaveData> chestStates)
    {
        chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        foreach(Chest chest in chests)
        {
            if (chest == null) continue;

            // DEBUG: See what IDs are being compared
            Debug.Log($"Scene Chest: {chest.chestID} | Looking for match in Save...");

            var saved = chestStates.Find(c => c.chestID == chest.chestID);
            if (saved != null)
            {
                Debug.Log($"<color=green>MATCH FOUND for {chest.chestID}! Setting Opened: {saved.isOpened}</color>");
                chest.SetOpened(saved.isOpened);
            }
            else
            {
                Debug.LogWarning($"<color=red>NO MATCH for {chest.chestID} in temp save data.</color>");
            }
            
        }
    }
    public Skill[] GetSkillsFromNames(List<string> skillNames)
    {
        if (skillNames == null || masterSkillDatabase == null) return new Skill[0];
        return skillNames.Select(name => masterSkillDatabase.Find(s => s.skillName == name))
                         .Where(s => s != null)
                         .ToArray();
    }
}
