using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class PartyPageManager : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform activePartyParent;  // Slots for indices 0-3
    public Transform reservePartyParent; // Slots for indices 4+
    public GameObject partySlotPrefab;

    private PartySlotUI firstSelectedSlot;

    void OnEnable()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (PartyManager.Instance == null) return;

        // Clear existing slots
        foreach (Transform child in activePartyParent) Destroy(child.gameObject);
        foreach (Transform child in reservePartyParent) Destroy(child.gameObject);

        List<Unit> allMembers = PartyManager.Instance.partyMembers;

        for (int i = 0; i < allMembers.Count; i++)
        {
            // First 4 go to Active, others go to Reserve
            Transform parent = (i < 4) ? activePartyParent : reservePartyParent;
            GameObject go = Instantiate(partySlotPrefab, parent);
            
            PartySlotUI slot = go.GetComponent<PartySlotUI>();
            slot.Setup(allMembers[i], (i < 4), this);

            // Disable the button for the leader (index 0)
            if (i == 0) 
            {
                go.GetComponent<Button>().interactable = false;
                // Optional: Change alpha or color to show it's "locked"
                go.GetComponent<CanvasGroup>().alpha = 0.7f; 
            }
        }

        // Auto-select the first slot for controller navigation
        StartCoroutine(RestoreSelection());
    }

    IEnumerator RestoreSelection()
    {
        yield return new WaitForEndOfFrame();
        if (activePartyParent.childCount > 0)
            EventSystem.current.SetSelectedGameObject(activePartyParent.GetChild(0).gameObject);
    }

    public void SelectTotem(PartySlotUI slot)
    {
        if (firstSelectedSlot == null)
        {
            firstSelectedSlot = slot;
            slot.UpdateVisuals();
        }
        else if (firstSelectedSlot == slot)
        {
            firstSelectedSlot = null;
            slot.UpdateVisuals();
        }
        else
        {
            PerformSwap(firstSelectedSlot, slot);
            firstSelectedSlot = null;
            RefreshUI();
        }
    }

    private void PerformSwap(PartySlotUI a, PartySlotUI b)
    {
        List<Unit> members = PartyManager.Instance.partyMembers;

        int indexA = members.IndexOf(a.assignedUnit);
        int indexB = members.IndexOf(b.assignedUnit);

        if (indexA == -1 || indexB == -1) return;

        // --- STRICT LEADER PROTECTION ---
        // If either unit is at index 0, block the swap entirely.
        if (indexA == 0 || indexB == 0)
        {
            Debug.Log("The leader's position is fixed and cannot be changed.");
            // Optional: Play a 'back' or 'error' sound here
            return; 
        }

        // Swap the units in the master list
        Unit temp = members[indexA];
        members[indexA] = members[indexB];
        members[indexB] = temp;
    }
    public bool IsFirstSelected(PartySlotUI slot) => firstSelectedSlot == slot;
}