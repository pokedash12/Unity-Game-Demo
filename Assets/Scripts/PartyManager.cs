using UnityEngine;
using System.Collections.Generic;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance;

    [Header("Party Members")]
    public List<Unit> partyMembers = new List<Unit>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call this if you recruit a new character during the game
    public void AddMember(Unit newMember)
    {
        if (!partyMembers.Contains(newMember))
        {
            partyMembers.Add(newMember);
        }
    }
}