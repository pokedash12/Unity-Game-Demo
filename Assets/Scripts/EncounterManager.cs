using UnityEngine;
using UnityEngine.InputSystem; // Using your project's Input System

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance;
    public static EncounterGroup encounterToSpawn;

    [Header("Configuration")]
    public float gracePeriod = 10f;      // Seconds of safety after a battle
    public float baseEncounterChance = 0.05f; // Initial growth rate
    public string battleSceneName = "Battle";

    [Header("Runtime State")]
    [SerializeField] private float currentDangerMeter = 0f;
    [SerializeField] private float lastEncounterTime;
    private Rigidbody2D playerRB;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        lastEncounterTime = Time.time;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerRB = player.GetComponent<Rigidbody2D>();
    }
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRB = player.GetComponent<Rigidbody2D>();
        }
    }

    public void UpdateEncounter(EncounterZone zone)
    {
        // 1. Check if we are in the grace period
        if (Time.time - lastEncounterTime < gracePeriod) return;
        if (playerRB == null)
        {
            FindPlayer();
            if (playerRB == null) return; // Still haven't found a player? Exit.
        }

        // 2. Check if the player is actually moving
        // (We don't want encounters if they are just standing still)
        if (playerRB == null || playerRB.linearVelocity.magnitude < 0.1f) return;

        // 3. Increase the danger meter based on time and the zone's multiplier
        // This makes the "inevitability" happen
        currentDangerMeter += baseEncounterChance * zone.encounterRateMultiplier * Time.deltaTime;

        // 4. Roll the dice (Random.value is 0.0 to 1.0)
        // As currentDangerMeter grows, this becomes more likely to succeed
        if (Random.value < currentDangerMeter * 0.01f) 
        {
            TriggerBattle(zone);
        }
    }

    private void TriggerBattle(EncounterZone zone)
    {
        Debug.Log("Random Encounter Triggered!");
        if (zone.possibleEncounters != null && zone.possibleEncounters.Length > 0)
        {
            int randomIndex = Random.Range(0, zone.possibleEncounters.Length);
            encounterToSpawn = zone.possibleEncounters[randomIndex];
        }
        
        // Reset state
        currentDangerMeter = 0f;
        lastEncounterTime = Time.time;

        // Use your existing SaveController to pack the player's data 
        // before we leave the scene
        SaveController saveController = FindAnyObjectByType<SaveController>();
        if (saveController != null)
        {
            saveController.TempSaveGame();
        }
        PauseController.SetPause(true);

        // Transition to battle
        SceneTransitionManager transition = FindAnyObjectByType<SceneTransitionManager>();
        if (transition != null)
        {
            SoundEffectManager.Play("BattleStart");
            MusicManager.Pause();
            transition.LoadScene(battleSceneName);
        }
    }
}