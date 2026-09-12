using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Linq;
using System.IO;

public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST, FLEE }

public class BattleSystem : MonoBehaviour
{
    public GameObject player;

    [Header("Target Selection UI")]
    public GameObject targetSelectionMenu;
    public List<Button> targetButtons;
    public List<TMP_Text> targetButtonTexts;

    // State tracking for the pending action
    private bool isPendingBasicAttack = false;
    private Skill pendingSkill = null;

    private bool enemyRealizedLowMP = false;
    public GameObject dialogBox;
    public GameObject selection;
    public GameObject skills;
	[Header("Enemy Management")]
    public Transform[] enemyStations; // Assign 4 positions in the Inspector
    public List<Unit> enemyUnits = new List<Unit>();
    private List<EnemyAI> enemyAIs = new List<EnemyAI>();
    public AudioClip punch;
    private bool isTyping;

    [SerializeField] private AudioClip audioClip;
    [SerializeField] private List<Button> skillButtons; 
    [SerializeField] private List<Button> selectionButtons; 
    [SerializeField] private List<TMP_Text> skillButtonTexts;
    [SerializeField] private TMP_Text skillDescriptionText;

    private List<Unit> allUnitsInTurnOrder = new List<Unit>();
    private int turnIndex = 0;

    [SerializeField] private string navSound;
    [SerializeField] private string selectSound;

    [SerializeField] private string enemySelectSound;

    [SerializeField] private string backSound;
    private GameObject lastSelectedObject;
    private float lastSoundTime = 0f;
    private float soundCooldown = 0.05f;
    
    public GameObject inventoryMenu; 
    
    [SerializeField] private List<Button> itemButtons;
    [SerializeField] private List<TMP_Text> itemButtonTexts;
    [SerializeField] public Transform[] partyStations;

	Unit playerUnit;
	Unit enemyUnit;
    public TMP_Text dialogueText;

	public BattleState state;
    private HashSet<Unit> weakenedEnemies = new HashSet<Unit>();

    public List<PlayerHUD> partyHUDs;

    private List<Unit> partyUnits = new List<Unit>();
    private int currentMemberTurnIndex = 0;
    [Header("Negotiation Tracking")]
    private bool isPendingTalk = false;
    private HashSet<Skill> partyUsedSkills = new HashSet<Skill>();
    private Dictionary<Unit, List<Skill>> enemyUsedSkills = new Dictionary<Unit, List<Skill>>();

    void Start()
    {
        state = BattleState.START;
        
        StartCoroutine(SetupBattle());
    }
    private void OpenTargetSelection()
    {
        selection.SetActive(false);
        skills.SetActive(false);
        targetSelectionMenu.SetActive(true);

        List<Button> activeButtons = new List<Button>();

        // 1. Enable/disable buttons based on health and reset listeners
        for (int i = 0; i < targetButtons.Count; i++)
        {
            targetButtons[i].onClick.RemoveAllListeners();

            if (i < enemyUnits.Count && enemyUnits[i].currentHP > 0)
            {
                targetButtons[i].gameObject.SetActive(true);
                targetButtonTexts[i].text = enemyUnits[i].unitName;
                
                int targetIndex = i; // This keeps the button linked to the correct enemy unit
                targetButtons[i].onClick.AddListener(() => OnTargetSelected(targetIndex));
                
                activeButtons.Add(targetButtons[i]);
            }
            else
            {
                targetButtons[i].gameObject.SetActive(false);
            }
        }

        // --- NEW FIX: Sort buttons by their physical X position ---
        // This ensures navigation (Left/Right) follows what the player sees on screen,
        // even if the list order in the Inspector is Station 2, 1, 3, 4.
        activeButtons = activeButtons
            .OrderBy(b => b.GetComponent<RectTransform>().position.x)
            .ToList();

        // 2. Dynamically link the navigation between active buttons in visual order
        for (int i = 0; i < activeButtons.Count; i++)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;

            // Link Right: Moves to the button physically to the right in our sorted list
            nav.selectOnRight = (i + 1 < activeButtons.Count) ? activeButtons[i + 1] : activeButtons[0];

            // Link Left: Moves to the button physically to the left in our sorted list
            nav.selectOnLeft = (i - 1 >= 0) ? activeButtons[i - 1] : activeButtons[activeButtons.Count - 1];

            activeButtons[i].navigation = nav;
        }

        // 3. Set focus to the leftmost available enemy
        if (activeButtons.Count > 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(activeButtons[0].gameObject);
        }
        
    }

    private void OnTargetSelected(int targetIndex)
    {
        targetSelectionMenu.SetActive(false);
        dialogBox.SetActive(true);

        Unit target = enemyUnits[targetIndex];
        PlayEnemySelectSound();

        // Fire the correct coroutine based on the pending state
        if (isPendingBasicAttack)
        {
            StartCoroutine(PlayerAttack(target));
        }
        else if (pendingSkill != null)
        {
            StartCoroutine(UseSkill(pendingSkill, target));
        }
        else if (isPendingTalk)
        {
            StartCoroutine(talk(target));
        }
        
        // Reset states
        isPendingTalk = false;
        isPendingBasicAttack = false;
        pendingSkill = null;
    }
    IEnumerator SetupBattle()
	{
        // 1. INSTANTIATE THE PREFAB FIRST so we have a real GameObject in the scene
        int memberCount = Mathf.Min(PartyManager.Instance.partyMembers.Count, 4);
    
        partyUnits.Clear();
        int activeCount = Mathf.Min(PartyManager.Instance.partyMembers.Count, 4);
        
        for (int i = 0; i < activeCount; i++)
        {
            partyUnits.Add(PartyManager.Instance.partyMembers[i]);
        }
        playerUnit = partyUnits[0];
        for (int i = 0; i < partyHUDs.Count; i++)
        {
            if (i < partyUnits.Count)
            {
                partyHUDs[i].gameObject.SetActive(true);
                
                partyHUDs[i].SETHUD(partyUnits[i]); 
            }
            else
            {
                partyHUDs[i].gameObject.SetActive(false);
            }
        }

        // Hide unused HUDs if party is less than 4
        for (int i = memberCount; i < partyHUDs.Count; i++)
            partyHUDs[i].gameObject.SetActive(false);
        enemyUnits.Clear();
        enemyAIs.Clear();
        partyUsedSkills.Clear();
        enemyUsedSkills.Clear();
        isPendingTalk = false;

        EncounterGroup encounter = EncounterManager.encounterToSpawn;
        if (encounter != null)
        {
            for (int i = 0; i < encounter.enemies.Length; i++)
            {
                // Limit to the number of stations you have (e.g., 4)
                if (i >= enemyStations.Length) break; 

                GameObject enemyGO = Instantiate(encounter.enemies[i], enemyStations[i].position, Quaternion.identity);
                enemyGO.transform.SetParent(enemyStations[i]);
                
                Unit eUnit = enemyGO.GetComponent<Unit>();
                enemyUnits.Add(eUnit);

                // Attach an independent AI to each enemy so they track their own Memory and MP
                EnemyAI enemyBrain = enemyGO.AddComponent<EnemyAI>();
                enemyBrain.ResetMemory();
                enemyAIs.Add(enemyBrain);
            }
        }
        EncounterManager.encounterToSpawn = null;

        // Adjust intro text for multiple enemies
        if (enemyUnits.Count > 1) {
            yield return StartCoroutine(TypeLine($"A group of enemies approaches!"));
        } else {
            yield return StartCoroutine(TypeLine($"A wild {enemyUnits[0].unitName} approaches..."));
        }

        allUnitsInTurnOrder.Clear();
        allUnitsInTurnOrder.AddRange(partyUnits);
        allUnitsInTurnOrder.AddRange(enemyUnits);

        // Sort by speed (highest first)
        SortTurnOrder();

        turnIndex = 0;
        ProcessNextTurn();
    }

    // Helper to sort the list (useful if speed changes mid-battle)
    void SortTurnOrder()
    {
        allUnitsInTurnOrder = allUnitsInTurnOrder
            .OrderByDescending(u => u.speed)
            .ThenByDescending(u => u.luck) // Luck as a tie-breaker
            .ToList();
    }
    void ProcessNextTurn()
    {
        if (AreAllEnemiesDead()) { StartCoroutine(EndBattle()); return; }
        if (partyUnits[0].currentHP <= 0) { state = BattleState.LOST; StartCoroutine(EndBattle()); return; }

        // Loop until we find a unit that is alive
        while (true)
        {
            if (turnIndex >= allUnitsInTurnOrder.Count)
            {
                turnIndex = 0;
                SortTurnOrder(); // Refresh order for the new "Round"
            }

            if (allUnitsInTurnOrder[turnIndex].currentHP > 0)
            {
                break; // Found a living unit!
            }

            turnIndex++; // Skip dead unit and try again
        }

        Unit currentUnit = allUnitsInTurnOrder[turnIndex];

        if (partyUnits.Contains(currentUnit))
        {
            currentMemberTurnIndex = partyUnits.IndexOf(currentUnit); 
            state = BattleState.PLAYERTURN;
            StartCoroutine(PlayerTurnRoutine());
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(SingleEnemyTurn(currentUnit));
        }
    }

    private bool skipLine;

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipLine = false;
        dialogueText.SetText("");
        
        yield return new WaitForSecondsRealtime(0.05f);

        foreach (char letter in line)
        {
            if (skipLine)
            {
                dialogueText.text = line;
                break;
            }

            dialogueText.text += letter;
            
            if (!char.IsWhiteSpace(letter))
            {
                SoundEffectManager.PlayVoice(audioClip);
            }
            
            yield return new WaitForSecondsRealtime(0.05f);
        }

        isTyping = false;

        // --- NEW FIX: Wait for one frame so the skip-press isn't counted as an advance-press ---
        yield return null; 

        // Now it will only advance on a NEW press after the typing is finished
        yield return new WaitUntil(() => Keyboard.current.zKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame);
    }

    public void UpdateSkillDescription(string text)
    {
        skillDescriptionText.text = text;
    }

    IEnumerator AnnounceTurn()
    {
        print("TO BE ANNOUNCED");
        if (currentMemberTurnIndex < partyUnits.Count)
            {
                print("HATH ANNOUNCED");
                yield return StartCoroutine(TypeLine("Choose action for " + partyUnits[currentMemberTurnIndex].unitName + ":"));
            }
    }
    void PlayerTurn()
    {
        StartCoroutine(PlayerTurnRoutine());
    }

    IEnumerator PlayerTurnRoutine()
    {
        // Skip dead party members
        if (partyUnits[currentMemberTurnIndex].currentHP <= 0)
        {
            NextMember();
            yield break;
        }

        dialogBox.SetActive(true);
        skills.SetActive(false);
        inventoryMenu.SetActive(false);
        selection.SetActive(false);

        print("ANNOUNCING");

        // 🔥 THIS is the key line
        yield return StartCoroutine(AnnounceTurn());

        // NOW switch UI after typing finishes
        dialogBox.SetActive(false);
        selection.SetActive(true);

        state = BattleState.PLAYERTURN;

        EventSystem.current.SetSelectedGameObject(selectionButtons[0].gameObject);
    }

    IEnumerator PlayerAttack(Unit target)
    {
        dialogueText.SetText("");
        yield return new WaitForSeconds(SoundEffectManager.GetClip(enemySelectSound).length * 0.75f);
        Unit actingUnit = partyUnits[currentMemberTurnIndex];
        bool isDead;
        selection.SetActive(false);
        dialogBox.SetActive(true);
        bool hits = BattleMath.CheckHit(actingUnit, target);
        if (!hits) {
            SoundEffectManager.Play("Punch");
            yield return StartCoroutine(TypeLine(actingUnit.unitName + " missed!"));
            NextMember();
        }
        else{
        bool isCrit = BattleMath.CheckCrit(actingUnit, target);
        int damage = BattleMath.CalculateDamage(actingUnit, target, false);
        SoundEffectManager.Play("Punch");
        AudioClip punchClip = SoundEffectManager.GetClip("Punch");

        if(isCrit)
        {
            damage = Mathf.RoundToInt(damage * 1.5f);
            StartCoroutine(ShakeEnemy(punchClip.length * 0.75f, 0.4f, enemyStations[enemyUnits.IndexOf(target)]));
            yield return StartCoroutine(TypeLine(actingUnit.unitName + " attacks " + target.unitName +"! They do " + damage * 2 + " critical damage!"));
            isDead = target.TakeDamage(damage * 2);
        }
        else
        {
            StartCoroutine(ShakeEnemy(punchClip.length * 0.75f, 0.2f, enemyStations[enemyUnits.IndexOf(target)]));
            yield return StartCoroutine(TypeLine(actingUnit.unitName + " attacks " + target.unitName +"! They do " + damage + " damage!")); 
            isDead = target.TakeDamage(damage);
        }     
        
        if (AreAllEnemiesDead())
        {
            state = BattleState.WON;
            StartCoroutine(EndBattle());
        }
        else
        {
            if(target.currentHP <= target.maxHP/4&& !weakenedEnemies.Contains(target) && !isDead)
            {
                yield return StartCoroutine(TypeLine(target.unitName + " is looking weakened."));
                weakenedEnemies.Add(target);
            }
            
            NextMember();
        }
        if (isDead)
        {
            StartCoroutine(FadeOutEnemy(target.gameObject, 0.2f));
        }
        }
    }

    IEnumerator SingleEnemyTurn(Unit currentEnemy)
    {
        int enemyListIndex = enemyUnits.IndexOf(currentEnemy);
        EnemyAI currentAI = enemyAIs[enemyListIndex];

        // Filter to only living party members to avoid attacking dead units or out-of-bounds indices
        List<Unit> livingParty = partyUnits.Where(p => p.currentHP > 0).ToList();

        if (livingParty.Count == 0) 
        {
            state = BattleState.LOST;
            StartCoroutine(EndBattle());
            yield break;
        }

        // Ask THIS specific    enemy's AI what to do
        var decision = currentAI.DetermineAction(currentEnemy, livingParty);
        Unit target = decision.target;
        Skill chosenSkill = decision.skill;
        if (target == null && decision.skill == null)
        {
            target = partyUnits[0];
        }

        if (target == null && chosenSkill != null) // Wasted turn due to MP discovery
        {
            yield return StartCoroutine(TypeLine($"{currentEnemy.unitName} tried to use {chosenSkill.skillName}, but is out of MP!"));
        }
        else if (chosenSkill != null) // Used a Skill
        {
            currentEnemy.currentMP -= chosenSkill.mp;
            if (!enemyUsedSkills.ContainsKey(currentEnemy)) {
                enemyUsedSkills[currentEnemy] = new List<Skill>();
            }
            enemyUsedSkills[currentEnemy].Add(chosenSkill);

            List<Unit> targets = new List<Unit>();
            if (chosenSkill.multiTarget) {
                targets = partyUnits.Where(p => p.currentHP > 0).ToList();
                yield return StartCoroutine(TypeLine($"{currentEnemy.unitName} used {chosenSkill.skillName} on the whole party!"));
            } else {
                targets.Add(target); // Normal single target
            }

            foreach(Unit t in targets)
            {
                int tIdx = partyUnits.IndexOf(t);
                bool doesHit = BattleMath.CheckHit(currentEnemy, t);

                if (!doesHit) {
                    SoundEffectManager.PlayVoice(chosenSkill.sfx, 1.25f);
                    yield return StartCoroutine(TypeLine($"{chosenSkill.skillName} missed {t.unitName}!"));
                    continue; // Skip to next target
                }
                
                bool isMagicSkill = chosenSkill.element != 'p' && chosenSkill.element != 'P';
                int damage = BattleMath.CalculateDamage(currentEnemy, t, isMagicSkill, chosenSkill.damage);
                bool isWeak = t.weaknesses.Contains(chosenSkill.element);
                bool isResist = t.resistances.Contains(chosenSkill.element);

                // AI LEARNS HERE for EVERY target hit
                currentAI.RecordResult(tIdx, chosenSkill.element, isWeak, isResist);

                if (isWeak) {
                    StartCoroutine(ShakePlayer(chosenSkill.sfx.length, 45, partyHUDs[tIdx].transform));
                    damage *= 2;
                } else if (isResist) {
                    StartCoroutine(ShakePlayer(chosenSkill.sfx.length, 15, partyHUDs[tIdx].transform));
                    damage /= 2;
                } else {
                    StartCoroutine(ShakePlayer(chosenSkill.sfx.length, 30, partyHUDs[tIdx].transform));
                }

                SoundEffectManager.PlayVoice(chosenSkill.sfx, 1.25f);
                t.TakeDamage(damage);
                partyHUDs[tIdx].SetHP(t.currentHP, t.maxHP);
                
                string effectText = isWeak ? " It's super effective!" : (isResist ? " It's not very effective..." : "");
                yield return StartCoroutine(TypeLine($"Hit {t.unitName} for {damage} damage!{effectText}"));
            }
        }
        else // Normal Attack
        {
            int tIdx = partyUnits.IndexOf(target);
            bool hits = BattleMath.CheckHit(currentEnemy, target);
            if (!hits) {
                SoundEffectManager.Play("Claw");
                yield return StartCoroutine(TypeLine($"{currentEnemy.unitName} missed!"));
            }
            else {
                SoundEffectManager.Play("Claw");
                AudioClip punchClip = SoundEffectManager.GetClip("Claw");
                ShakePlayer(punchClip.length, 20, partyHUDs[tIdx].transform);
                int damage = BattleMath.CalculateDamage(currentEnemy, target, false);
                bool isCrit = BattleMath.CheckCrit(currentEnemy, target);
                if (isCrit)
                {
                    StartCoroutine(ShakePlayer(punchClip.length, 40, partyHUDs[tIdx].transform));
                    damage *= 2;
                }
                else
                {
                    StartCoroutine(ShakePlayer(punchClip.length, 20, partyHUDs[tIdx].transform));
                }

                target.TakeDamage(damage);
                partyHUDs[tIdx].SetHP(target.currentHP, target.maxHP);
                yield return StartCoroutine(TypeLine($"{currentEnemy.unitName} attacks {target.unitName} for {damage} damage!"));
            }
        }
    
        // If the main character dies during the onslaught, end the phase immediately
        if (partyUnits[0].currentHP <= 0)
        {
            state = BattleState.LOST;
            StartCoroutine(EndBattle());
            yield break;
        }
        turnIndex++;
        ProcessNextTurn();
    }

    private bool AreAllEnemiesDead()
    {
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0) return false;
        }
        return true;
    }

    private void CheckForLossOrContinue()
    {
        // Rule: If the main character (Index 0) is dead, the game is over immediately.
        if (partyUnits[0].currentHP <= 0)
        {
            state = BattleState.LOST;
            StartCoroutine(EndBattle());
        }
        else
        {
            // If the hero is alive, the player gets their turn back.
            state = BattleState.PLAYERTURN;
            PlayerTurn();
        }
    }

    IEnumerator ShakeEnemy(float duration, float magnitude, Transform target)
    {
        Vector3 originalPos = target.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float decay = 1.0f - (elapsed / duration);
            float currentMagnitude = magnitude * decay;

            float x = UnityEngine.Random.Range(-1f, 1f) * currentMagnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * currentMagnitude * 0.2f;

            target.position= new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null; 
        }

        target.position = originalPos;
    }

    IEnumerator ShakePlayer(float duration, float magnitude, Transform target)
    {
        RectTransform rectTarget = target.GetComponent<RectTransform>();
        if (rectTarget == null) yield break;

        Vector2 originalAnchoredPos = rectTarget.anchoredPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float decay = 1.0f - (elapsed / duration);
            float currentMagnitude = magnitude * decay;

            float x = UnityEngine.Random.Range(-1f, 1f) * currentMagnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * currentMagnitude * 0.2f;

            rectTarget.anchoredPosition = originalAnchoredPos + new Vector2(x, y);

            elapsed += Time.deltaTime;
            yield return null; 
        }

        rectTarget.anchoredPosition = originalAnchoredPos;
    }

    IEnumerator EndBattle()
    {
        if(state == BattleState.WON)
        {
            for(int i = 0; i < partyUnits.Count; i++)
            {
                partyUnits[i].currentHP = partyUnits[i].maxHP;
                if (i < PartyManager.Instance.partyMembers.Count)
                {
                    PartyManager.Instance.partyMembers[i].currentHP = partyUnits[i].maxHP;
                }
            }
            yield return StartCoroutine(TypeLine("YOU WON"));
        }
        else if (state == BattleState.LOST)
        {
            for(int i = 0; i < partyUnits.Count; i++)
            {
                partyUnits[i].currentHP = partyUnits[i].maxHP;
                if (i < PartyManager.Instance.partyMembers.Count)
                {
                    PartyManager.Instance.partyMembers[i].currentHP = partyUnits[i].maxHP;
                }
            }
            yield return StartCoroutine(TypeLine("YOU LOST YOU BUFFOON"));
        }
        else if (state == BattleState.FLEE)
        {
            for(int i = 0; i < partyUnits.Count; i++)
            {
                partyUnits[i].currentHP = partyUnits[i].maxHP;
                if (i < PartyManager.Instance.partyMembers.Count)
                {
                    PartyManager.Instance.partyMembers[i].currentHP = partyUnits[i].maxHP;
                }
            }
            yield return StartCoroutine(TypeLine("You got away safely."));
        }

        SceneTransitionManager sceneTransitionManager = FindAnyObjectByType<SceneTransitionManager>();
        if (sceneTransitionManager != null)
        {
            SaveController saveController = FindAnyObjectByType<SaveController>();
            if (saveController != null)
            {
                // CRITICAL FIX: Tell SaveController to grab the data from THIS battle's player instance
                // before writing it to the JSON file. Otherwise it saves the old Overworld player data.
                saveController.allPartyData = PackPartyData();
                saveController.TempSaveGame2();
            }
            else
            {
                Debug.LogError("SaveController instance not found.");
            }
            sceneTransitionManager.LoadScene("Overworld");
        }
        else
        {
            Debug.LogError("SceneTransitionManager instance not found.");
        }
    }
    private int GetDynamicMPCost(Unit actingUnit, Skill skill)
    {
        int baseCost = skill.mp; // Change 'mpCost' if your Skill script uses a different name

        // 1. The Main Player (Index 0) always pays normal cost
        if (actingUnit == PartyManager.Instance.partyMembers[0])
        {
            return baseCost;
        }

        // 2. Convert the skill element to lowercase so 'F' and 'f' both work safely
        char skillElement = char.ToLower(skill.element);

        // 3. Proficient (Half Cost, minimum of 1)
        if (actingUnit.resistances != null && actingUnit.resistances.Contains(skillElement))
        {
            return Mathf.Max(1, baseCost / 2); 
        }

        // 4. Weakness (Double Cost)
        if (actingUnit.weaknesses != null && actingUnit.weaknesses.Contains(skillElement))
        {
            return baseCost * 2;
        }

        // 5. Neutral (Normal Cost)
        return baseCost;
    }
    private List<PlayerSaveData> PackPartyData()
    {
        List<PlayerSaveData> allPartyData = new List<PlayerSaveData>();
        
        foreach (Unit member in partyUnits)
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

    public void onTalkButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        PlaySelectSound();
        
        // Save state for target selection
        isPendingBasicAttack = false;
        pendingSkill = null;
        isPendingTalk = true; 
        
        OpenTargetSelection();
    }

    public IEnumerator talk(Unit targetEnemy)
    {
        dialogBox.SetActive(true);
        selection.SetActive(false);
        targetSelectionMenu.SetActive(false);

        yield return StartCoroutine(TypeLine($"You try to reason with {targetEnemy.unitName}..."));
        yield return StartCoroutine(TypeLine($"..."));

        // 1. Check Receptiveness (Are there other healthy enemies?)
        int healthyOthers = enemyUnits.Count(e => e != targetEnemy && e.currentHP > 0 && !weakenedEnemies.Contains(e));
        bool isReceptive = (healthyOthers == 0) || (UnityEngine.Random.value <= 0.25f);

        // 2. Base check for them to listen
        if (!isReceptive || targetEnemy.currentHP > targetEnemy.maxHP / 2)
        {
            yield return StartCoroutine(TypeLine($"{targetEnemy.unitName} ignores your words and counter-attacks!"));
            state = BattleState.ENEMYTURN;
            yield return StartCoroutine(SingleEnemyTurn(targetEnemy));
            yield break;
        }

        // 3. Filter skills the player doesn't know (or hasn't used)
        List<Skill> unknownSkills = targetEnemy.skills.Where(s => s != null && !partyUsedSkills.Contains(s)).ToList();
        
        if (unknownSkills.Count == 0)
        {
            yield return StartCoroutine(TypeLine($"{targetEnemy.unitName} has nothing new they can show you right now."));
            NextMember();
            yield break;
        }

        // 4. Filter unknown skills by the Enemy's current MP
        List<Skill> teachableSkills = unknownSkills.Where(s => targetEnemy.currentMP >= s.mp).ToList();

        if (teachableSkills.Count == 0)
        {
            // The enemy knows things you don't, but lacks the energy to perform them
            yield return StartCoroutine(TypeLine($"{targetEnemy.unitName} is too exhausted to teach any skills right now."));
            NextMember();
            yield break;
        }

        // 5. Weighted Random Selection (Favor skills used this battle)
        List<Skill> selectionPool = new List<Skill>();
        foreach(Skill s in teachableSkills)
        {
            selectionPool.Add(s); 
            if (enemyUsedSkills.ContainsKey(targetEnemy) && enemyUsedSkills[targetEnemy].Contains(s))
            {
                selectionPool.Add(s);
                selectionPool.Add(s);
                selectionPool.Add(s); 
            }
        }
        
        Skill skillToTeach = selectionPool[UnityEngine.Random.Range(0, selectionPool.Count)];

        // 6. Final Player-Side Checks (Already know it / Level / MP)
        if (playerUnit.skills.Contains(skillToTeach) || playerUnit.allSkills.Contains(skillToTeach))
        {
            yield return StartCoroutine(TypeLine($"{targetEnemy.unitName} tries to teach you {skillToTeach.skillName}."));
            yield return StartCoroutine(TypeLine($"...but you already know how to use {skillToTeach.skillName}."));
            NextMember();
            yield break;
        }

        if (playerUnit.currentMP < skillToTeach.mp)
        {
            yield return StartCoroutine(TypeLine($"{targetEnemy.unitName} tries to show you {skillToTeach.skillName}, but you lack the {skillToTeach.mp} MP to follow along!"));
            NextMember();
            yield break;
        }

        // 7. SUCCESS! 
        // The player pays the cost to learn it
        playerUnit.currentMP -= skillToTeach.mp;
        partyHUDs[0].SetMP(playerUnit.currentHP, playerUnit.maxHP);
        playerUnit.addSkill(skillToTeach);
        
        yield return StartCoroutine(TypeLine($"{targetEnemy.unitName} teaches you {skillToTeach.skillName} and departs."));

        // 8. THE ENEMY LEAVES
        // Use your existing fade logic and mark them as "dead" so they don't take turns
        targetEnemy.currentHP = 0; 
        yield return StartCoroutine(FadeOutEnemy(targetEnemy.gameObject, 0.3f));

        // 9. Check for Battle End
        if (AreAllEnemiesDead())
        {
            state = BattleState.WON;
            StartCoroutine(EndBattle());
        }
        else
        {
            NextMember();
        }
    }

    public void onAttackButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        PlaySelectSound();
        
        // Save state
        isPendingBasicAttack = true;
        pendingSkill = null;
        
        OpenTargetSelection();
    }

    public void OnFleeButton()
    {
        PlaySelectSound();
        if (state != BattleState.PLAYERTURN) return;

        // 1. Calculate Average Party Speed
        float avgPartySpeed = 0;
        foreach (Unit unit in partyUnits)
        {
            avgPartySpeed += unit.speed;
        }
        avgPartySpeed /= partyUnits.Count;

        // 2. Calculate Average Enemy Speed
        float avgEnemySpeed = 0;
        int livingEnemies = 0;
        foreach (Unit enemy in enemyUnits)
        {
            if (enemy.currentHP > 0)
            {
                avgEnemySpeed += enemy.speed;
                livingEnemies++;
            }
        }
        if (livingEnemies > 0) avgEnemySpeed /= livingEnemies;

        // 3. Determine Chance (Base 50%, adjusted by speed ratio)
        // Formula: 0.5 * (PartySpeed / EnemySpeed)
        float fleeChance = 0.5f * (avgPartySpeed / Math.Max(1, avgEnemySpeed));
        
        fleeChance = Mathf.Clamp(fleeChance, 0.02f, 1.0f);

        StartCoroutine(HandleFlee(fleeChance));
    }

    IEnumerator HandleFlee(float successChance)
    {
        selection.SetActive(false);
        dialogBox.SetActive(true);

        yield return StartCoroutine(TypeLine("Attempting to flee..."));
        yield return StartCoroutine(TypeLine("..."));

        if (UnityEngine.Random.value <= successChance)
        {            
            // Use your existing transition logic
            state = BattleState.FLEE;
            StartCoroutine(EndBattle());
        }
        else
        {
            yield return StartCoroutine(TypeLine("Couldn't get away!"));
            NextMember();
        }
    }

    public void onSkillButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        PlaySelectSound();

        Unit actingUnit = partyUnits[currentMemberTurnIndex];

        selection.SetActive(false);
        dialogBox.SetActive(false);
        skills.SetActive(true);
        
        for (int i = 0; i < skillButtons.Count; i++)
        {
            skillButtons[i].gameObject.SetActive(true);
            int index = i;
            skillButtons[i].onClick.RemoveAllListeners();

            if (i < actingUnit.skills.Length && actingUnit.skills[i] != null)
            {
                Skill skill = actingUnit.skills[i];
                skillButtonTexts[i].text = skill.skillName + "   " + GetDynamicMPCost(actingUnit, skill) + " MP";
                
                if(actingUnit.currentMP-GetDynamicMPCost(actingUnit, skill)>= 0)
                {
                    skillButtons[i].interactable = true;
                    skillButtons[i].onClick.AddListener(() => OnSkillSelected(index));
                }
                else
                {
                    skillButtons[i].interactable = false;
                    skillButtons[i].onClick.RemoveAllListeners();
                }
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(skillButtons[0].gameObject);
    }



    void OnSkillSelected(int index)
    {
        if (state != BattleState.PLAYERTURN) return;
        Unit actingUnit = partyUnits[currentMemberTurnIndex];
        Skill skill = actingUnit.skills[index];
        PlaySelectSound();
        
        if (skill.element == 'h' || skill.element == 'H')
        {
            skills.SetActive(false);
            dialogBox.SetActive(true);
            StartCoroutine(UseSkill(skill, partyUnits[0])); 
            return;
        }

        // NEW: Bypass target selection for Multi-Target skills
        if (skill.multiTarget)
        {
            skills.SetActive(false);
            dialogBox.SetActive(true);
            StartCoroutine(UseSkill(skill, null)); // Null indicates AoE
            return;
        }

        pendingSkill = skill;
        isPendingBasicAttack = false;
        
        OpenTargetSelection();
    }
    IEnumerator FadeOutEnemy(GameObject enemyGO, float duration)
    {
        // Try to find the SpriteRenderer on the enemy or its children
        SpriteRenderer sr = enemyGO.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        Color startColor = sr.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Optionally disable the object entirely after it's invisible
        enemyGO.SetActive(false);
    }

    IEnumerator UseSkill(Skill skill, Unit target)
    {
        dialogueText.SetText("");
        yield return new WaitForSeconds(SoundEffectManager.GetClip(enemySelectSound).length * 0.75f);
        Unit actingUnit = partyUnits[currentMemberTurnIndex]; 
        actingUnit.currentMP -= GetDynamicMPCost(actingUnit, skill);
        partyHUDs[currentMemberTurnIndex].SetMP(actingUnit.currentMP, actingUnit.maxMP);
        dialogBox.SetActive(true);
        skills.SetActive(false);
        
        AudioClip punchClip = skill.sfx;
        partyUsedSkills.Add(skill);

        if(skill.element == 'h')
        {
            SoundEffectManager.PlayVoice(skill.sfx, 1.25f);
            int heal = BattleMath.CalculateHealing(actingUnit, skill.damage);
            playerUnit.currentHP += heal;
            partyHUDs[0].SetHP(playerUnit.currentHP, playerUnit.maxHP);
            yield return StartCoroutine(TypeLine(playerUnit.unitName + " healed for " + heal + " HP."));
            NextMember();
            yield break;
        }

        // Establish who is getting hit
        List<Unit> targets = new List<Unit>();
        if (skill.multiTarget) {
            targets = enemyUnits.Where(e => e.currentHP > 0).ToList();
            yield return StartCoroutine(TypeLine(actingUnit.unitName + " used " + skill.skillName + " on all enemies!"));
        } else {
            targets.Add(target);
        }

        // Loop through all determined targets
        foreach (Unit t in targets)
        {
            bool hits = BattleMath.CheckHit(actingUnit, t);
            if (!hits) {
                SoundEffectManager.PlayVoice(skill.sfx, 1.25f);
                yield return StartCoroutine(TypeLine("It missed " + t.unitName + "!"));
                continue;
            } 
            
            bool isMagicSkill = skill.element != 'p' && skill.element != 'P';
            int damage = BattleMath.CalculateDamage(actingUnit, t, isMagicSkill, skill.damage);
            bool isDead = false;
            
            if(t.weaknesses.Contains(skill.element))
            {
                SoundEffectManager.PlayVoice(skill.sfx, 1.5f);
                StartCoroutine(ShakeEnemy(punchClip.length * 0.75f, 0.45f, enemyStations[enemyUnits.IndexOf(t)]));
                isDead = t.TakeDamage(damage * 2);
                yield return StartCoroutine(TypeLine("Dealt " + damage * 2 + " to " + t.unitName + "! Super effective!"));
            }
            else if (t.resistances.Contains(skill.element))
            {
                SoundEffectManager.PlayVoice(skill.sfx, 1f);
                StartCoroutine(ShakeEnemy(punchClip.length * 0.75f, 0.15f, enemyStations[enemyUnits.IndexOf(t)]));
                isDead = t.TakeDamage(damage / 2);
                yield return StartCoroutine(TypeLine("Dealt " + damage / 2 + " to " + t.unitName + ". Not very effective."));
            }
            else
            {
                SoundEffectManager.PlayVoice(skill.sfx, 1.25f);
                StartCoroutine(ShakeEnemy(punchClip.length * 0.75f, 0.3f, enemyStations[enemyUnits.IndexOf(t)]));
                isDead = t.TakeDamage(damage);
                yield return StartCoroutine(TypeLine("Dealt " + damage + " to " + t.unitName + "!"));
            }

            if(t.currentHP <= t.maxHP/4 && !weakenedEnemies.Contains(t) && !isDead)
            {
                yield return StartCoroutine(TypeLine(t.unitName + " is looking weakened."));
                weakenedEnemies.Add(t);
            }

            if (isDead) { StartCoroutine(FadeOutEnemy(t.gameObject, 0.2f)); }
        }

        if (AreAllEnemiesDead()) {
            state = BattleState.WON;
            StartCoroutine(EndBattle());
        } else {
            NextMember();
        }
    }

    public void OnSkillHighlighted(int index)
    {
        Unit actingUnit = partyUnits[currentMemberTurnIndex];
        if (index >= actingUnit.skills.Length || actingUnit.skills[index] == null)
        {
            skillDescriptionText.text = "";
            return;
        }
        Skill skill = actingUnit.skills[index];
        if (skill != null)
        {
            skillDescriptionText.text = skill.description;
        }
        else
        {
            skillDescriptionText.text = "";
        }
    }
    public void OnItemHighlighted(int itemID)
    {
        ItemDict dict = FindAnyObjectByType<ItemDict>();
        Item item = dict.GetItemData(itemID);

        if (item != null)
        {
            // Reuse the same text box you use for skills
            skillDescriptionText.text = item.description; 
        }
        else
        {
            skillDescriptionText.text = "";
        }
    }

    void Update()
    {
        // --- NEW: Audio Navigation Tracking ---
        if (EventSystem.current != null)
        {
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            
            if (currentSelected != null && currentSelected != lastSelectedObject)
            {
                // Only play the sound if one of the menus is actually active 
                // (Prevents sounds from playing during enemy turn or while dialog is typing)
                if (selection.activeSelf || skills.activeSelf || inventoryMenu.activeSelf|| targetSelectionMenu.activeSelf)
                {
                    PlayNavSound();
                }
                lastSelectedObject = currentSelected;
            }
            else if (currentSelected == null)
            {
                lastSelectedObject = null;
            }
        }

        if (isTyping && Keyboard.current.zKey.wasPressedThisFrame)
        {
            skipLine = true;
        }

        if (state != BattleState.PLAYERTURN) return;

        if (skills.activeSelf || inventoryMenu.activeSelf || targetSelectionMenu.activeSelf) 
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame)
            {
                PlayBackSound(); // <-- SOUND: Triggered when backing out of a menu
                if (targetSelectionMenu.activeSelf)
                {
                    targetSelectionMenu.SetActive(false);
                    if (isPendingBasicAttack) 
                    {
                        selection.SetActive(true); // Go back to Attack/Skill/Bag
                        isPendingBasicAttack = false;
                        EventSystem.current.SetSelectedGameObject(selectionButtons[0].gameObject);
                    }
                    else if (isPendingTalk) 
                    {
                        selection.SetActive(true); // Go back to Attack/Skill/Bag
                        isPendingTalk = false;
                        EventSystem.current.SetSelectedGameObject(selectionButtons[3].gameObject);
                    }
                    else 
                    {
                        skills.SetActive(true); // Go back to Skill list
                        EventSystem.current.SetSelectedGameObject(skillButtons[0].gameObject);
                    }
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
        }
    }
    public void PlaySelectSound()
    {
        if (selectSound != null && Time.time - lastSoundTime > soundCooldown)
        {
            SoundEffectManager.Play(selectSound);
            lastSoundTime = Time.time;
        }
    }

    public void PlayEnemySelectSound()
    {
        if (enemySelectSound != null && Time.time - lastSoundTime > soundCooldown)
        {
            Debug.Log("Playing Enemy Select: " + enemySelectSound); // Debug to verify
            SoundEffectManager.Play(enemySelectSound);
            lastSoundTime = Time.time;
        }
    }

    public void PlayBackSound()
    {
        if (backSound != null && Time.time - lastSoundTime > soundCooldown)
        {
            SoundEffectManager.Play(backSound);
            lastSoundTime = Time.time;
        }
    }

    public void PlayNavSound()
    {
        if (navSound != null && Time.time - lastSoundTime > soundCooldown)
        {
            SoundEffectManager.Play(navSound);
            lastSoundTime = Time.time;
        }
    }

    void ReturnToMainMenu()
    {
        skills.SetActive(false);
        dialogBox.SetActive(false);
        selection.SetActive(true);
        inventoryMenu.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectionButtons[0].gameObject);
    }

    public void OnBagButton() 
    {
        if (state != BattleState.PLAYERTURN) return;
        PlaySelectSound();

        selection.SetActive(false);
        inventoryMenu.SetActive(true);

        InventoryController inv = FindAnyObjectByType<InventoryController>();
        ItemDict dict = FindAnyObjectByType<ItemDict>();
        List<InventorySaveData> items = inv.GetInventoryItems();

        for (int i = 0; i < itemButtons.Count; i++)
        {
            itemButtons[i].onClick.RemoveAllListeners();
            // Clear existing triggers to avoid stacking sounds/logic
            EventTrigger trigger = itemButtons[i].gameObject.GetComponent<EventTrigger>() ?? itemButtons[i].gameObject.AddComponent<EventTrigger>();
            trigger.triggers.Clear();

            if (i < items.Count)
            {
                int index = i;
                Item data = dict.GetItemData(items[i].itemID);
                itemButtonTexts[i].text = $"{data.itemName} x{items[i].count}";
                itemButtons[i].interactable = true;
                
                // Click Logic
                itemButtons[i].onClick.AddListener(() => OnItemSelected(items[index].itemID));

                // Highlight Logic (Hover/Select)
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Select; // Works for Controller/Keyboard nav
                entry.callback.AddListener((eventData) => { OnItemHighlighted(items[index].itemID); });
                trigger.triggers.Add(entry);
            }
            else
            {
                itemButtonTexts[i].text = "--";
                itemButtons[i].interactable = false;
            }
}
        EventSystem.current.SetSelectedGameObject(itemButtons[0].gameObject);
    }

    void OnItemSelected(int itemID)
    {
        inventoryMenu.SetActive(false);
        dialogBox.SetActive(true);
        skillDescriptionText.text = "";
        
        
        ItemDict dict = FindAnyObjectByType<ItemDict>();
        Item item = dict.GetItemData(itemID);

        SoundEffectManager.PlayVoice(item.sfx);

        if (item.effect == 'h') 
        {
            playerUnit.currentHP += item.potency;
            partyHUDs[0].SetHP(playerUnit.currentHP, playerUnit.maxHP);
        }

        FindAnyObjectByType<InventoryController>().RemoveItem(itemID, 1);
        
        StartCoroutine(UseItemRoutine(item.itemName));
    }

    void NextMember()
    {
        turnIndex++;
        ProcessNextTurn();
    }

    IEnumerator UseItemRoutine(string itemName)
    {
        Unit actingUnit = partyUnits[currentMemberTurnIndex];
        yield return StartCoroutine(TypeLine($"{actingUnit.unitName} used {itemName}!"));
        turnIndex++;
        ProcessNextTurn();
    }

    
}