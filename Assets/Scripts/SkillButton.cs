using UnityEngine;
using UnityEngine.EventSystems;

public class SkillSelectHandler : MonoBehaviour, ISelectHandler
{
    public int index;
    public BattleSystem battleSystem;

    public void OnSelect(BaseEventData eventData)
    {
        battleSystem.OnSkillHighlighted(index);
    }
}