using UnityEngine;

[System.Serializable]
public class EncounterGroup
{
    public string encounterName; // e.g., "Goblin Patrol"
    public GameObject[] enemies; // Drag 1 to 4 enemy prefabs here in the Inspector
}

public class EncounterZone : MonoBehaviour
{
    [Header("Area Settings")]
    public string areaName;
    public string areaMusic; 
    public float encounterRateMultiplier = 1.0f;
    
    // NEW: Replace possibleEnemies with possibleEncounters
    public EncounterGroup[] possibleEncounters;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EncounterManager.Instance.UpdateEncounter(this);
        }
    }
}