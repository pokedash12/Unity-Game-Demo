using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using NUnit.Framework;

public class StatsPageManager : MonoBehaviour
{
    [Header("Party Management")]
    [Tooltip("Add your Player and Party Members here. Index 0 is the Main Player (shares their allSkills pool).")]
    public List<Unit> partyMembers;
    private int currentMemberIndex = 0;

    [Header("Stats UI")]
    public TMP_Text nameText;
    public TMP_Text strongAndWeak;
    public TMP_Text hpText, mpText, levelText, dmgText, values;

    [Header("Skill Management")]
    public Transform activeSkillsParent;
    public Transform allSkillsParent;
    public GameObject skillSlotPrefab;

    [Header("Skill Text Customization")]
    public bool useCustomTextSettings = false;
    public TMP_FontAsset customFont;
    public float customFontSize = 24f;
    public Color customFontColor = Color.white;

    private SkillUISlot firstSelectedSlot;

    void OnEnable()
    {
        // Automatically link the UI to the global PartyManager
        if (PartyManager.Instance != null)
        {
            partyMembers = PartyManager.Instance.partyMembers;
        }
        
        currentMemberIndex = 0;
        RefreshUI();
    }

    // --- TAB SWITCHING METHODS --- //
    // Hook these up to UI Buttons' OnClick events to create your tabs
    
    public void NextPartyMember()
    {
        if (partyMembers == null || partyMembers.Count <= 1) return;
        currentMemberIndex = (currentMemberIndex + 1) % partyMembers.Count;
        ResetSwapState();
        RefreshUI();
    }

    public void PrevPartyMember()
    {
        if (partyMembers == null || partyMembers.Count <= 1) return;
        currentMemberIndex--;
        if (currentMemberIndex < 0) currentMemberIndex = partyMembers.Count - 1;
        ResetSwapState();
        RefreshUI();
    }

    public void SwitchToSpecificMember(int index)
    {
        if (partyMembers == null || index < 0 || index >= partyMembers.Count) return;
        currentMemberIndex = index;
        ResetSwapState();
        RefreshUI();
    }

    private void ResetSwapState()
    {
        firstSelectedSlot = null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    // ---------------------------- //

    public void RefreshUI() => RefreshUI(0, true);
    
    private string getStrengths(int indexToSelect)
    {
        string list = "";
        if(indexToSelect == 0)
        {
            return "NONE";
        }
        else
        {
            foreach (char strength in partyMembers[currentMemberIndex].resistances)
            {
                list += $"{getElem(strength)}, ";
            }
            return list.Substring(0, list.Length - 2);
        }
    }
    private string getWeaknesses(int indexToSelect)
    {
        string list = "";
        if(indexToSelect == 0)
        {
            return "NONE";
        }
        else
        {
            foreach (char strength in partyMembers[currentMemberIndex].weaknesses)
            {
                list += $"{getElem(strength)}, ";
            }
            return list.Substring(0, list.Length - 2);
        }
    }

    private string getElem(char elem)
    {
        switch (elem)
        {
            case 'f':
                return ("FIRE");
            case 'l':
                return ("ELECTRIC");
            case 'h':
                return ("HEAL");
            case 'e':
                return ("EARTH");
            case 'i':
                return ("ICE");
            case 'p':
                return ("PHYSICAL");
            case 'w':
                return ("WIND");
        }

        return "NONE";
    }
    public void RefreshUI(int indexToSelect, bool wasActive)
    {
        if (partyMembers == null || partyMembers.Count == 0) return;

        Unit currentUnit = partyMembers[currentMemberIndex];
        Unit sharedLibraryUnit = partyMembers[0]; // Everyone shares the Main Player's skill pool

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        foreach (Transform child in activeSkillsParent) Destroy(child.gameObject);
        foreach (Transform child in allSkillsParent) Destroy(child.gameObject);

        // Update Stats specific to the current tab
        nameText.text = currentUnit.unitName;
        levelText.text = "LVL: " + currentUnit.unitLevel;
        hpText.text = $"HP: {currentUnit.currentHP}/{currentUnit.maxHP}";
        mpText.text = $"MP: {currentUnit.currentMP}/{currentUnit.maxMP}";
        strongAndWeak.text = $"PROFICIENCIES: {getStrengths(currentMemberIndex)}\nWEAKNESSES: {getWeaknesses(currentMemberIndex)}";
        values.text = $"STR: {currentUnit.strength}      SPD: {currentUnit.speed}\nDEF: {currentUnit.defense}      LUC: {currentUnit.luck}\nMAG: {currentUnit.magic}";

        // Build Active Skills for the CURRENT unit
        for (int i = 0; i < 6; i++)
        {
            GameObject go = Instantiate(skillSlotPrefab, activeSkillsParent);
            Skill s = (currentUnit.skills != null && i < currentUnit.skills.Length) ? currentUnit.skills[i] : null;
            go.GetComponent<SkillUISlot>().Setup(s, true, this);
        }

        // Build Library Skills from the SHARED pool
        if (sharedLibraryUnit.allSkills != null)
        {
            foreach (Skill s in sharedLibraryUnit.allSkills)
            {
                if (s == null) continue;
                GameObject go = Instantiate(skillSlotPrefab, allSkillsParent);
                go.GetComponent<SkillUISlot>().Setup(s, false, this);
            }
        }

        StartCoroutine(RestoreSelection(indexToSelect, wasActive));
    }

    IEnumerator RestoreSelection(int index, bool wasActive)
    {
        yield return new WaitForEndOfFrame();
        Transform targetParent = wasActive ? activeSkillsParent : allSkillsParent;
        
        if (index >= 0 && index < targetParent.childCount)
        {
            EventSystem.current.SetSelectedGameObject(targetParent.GetChild(index).gameObject);
        }
    }

    public bool IsFirstSelected(SkillUISlot slot) => firstSelectedSlot == slot;
    public bool IsSwapInProgress() => firstSelectedSlot != null;

    public void SelectSkill(SkillUISlot slot)
    {
        int clickedIndex = slot.transform.GetSiblingIndex();
        bool clickedWasActive = slot.isActiveSlot;

        if (firstSelectedSlot == null)
        {
            if (slot.assignedSkill != null || slot.isActiveSlot)
            {
                firstSelectedSlot = slot;
                RefreshAllVisuals(); 
            }
        }
        else
        {
            if (firstSelectedSlot == slot)
            {
                firstSelectedSlot = null;
                RefreshAllVisuals();
                return;
            }

            if (!firstSelectedSlot.isActiveSlot && !slot.isActiveSlot)
            {
                firstSelectedSlot = slot;
                RefreshAllVisuals();
                return;
            }

            PerformSwap(firstSelectedSlot, slot);
            firstSelectedSlot = null;
            
            RefreshUI(clickedIndex, clickedWasActive);
        }
    }

    private void RefreshAllVisuals()
    {
        foreach (Transform child in activeSkillsParent) child.GetComponent<SkillUISlot>().UpdateVisuals();
        foreach (Transform child in allSkillsParent) child.GetComponent<SkillUISlot>().UpdateVisuals();
    }

    private void PerformSwap(SkillUISlot a, SkillUISlot b)
    {
        Unit currentUnit = partyMembers[currentMemberIndex]; // Only modify the active tab's unit

        List<Skill> activeList = currentUnit.skills.ToList();
        while (activeList.Count < 6) activeList.Add(null);

        int idxA = a.transform.GetSiblingIndex();
        int idxB = b.transform.GetSiblingIndex();

        if (a.assignedSkill != null && b.assignedSkill != null && a.assignedSkill == b.assignedSkill)
        {
            if (a.isActiveSlot) activeList[idxA] = null;
            if (b.isActiveSlot) activeList[idxB] = null;
            currentUnit.skills = activeList.ToArray();
            return;
        }

        if (a.isActiveSlot && b.isActiveSlot) 
        {
            Skill temp = activeList[idxA];
            activeList[idxA] = activeList[idxB];
            activeList[idxB] = temp;
        }
        else 
        {
            int activeIdx = a.isActiveSlot ? idxA : idxB;
            Skill skillToEquip = a.isActiveSlot ? b.assignedSkill : a.assignedSkill;

            if (skillToEquip != null && activeList.Contains(skillToEquip))
            {
                int existingIdx = activeList.IndexOf(skillToEquip);
                activeList[existingIdx] = activeList[activeIdx]; 
            }

            activeList[activeIdx] = skillToEquip;
        }

        currentUnit.skills = activeList.ToArray(); // Save back to the current unit
    }
}