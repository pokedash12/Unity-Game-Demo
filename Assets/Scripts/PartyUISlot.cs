using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PartySlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text unitNameText;
    public TMP_Text levelText;
    public Unit assignedUnit;
    public bool isActiveSlot;
    
    private PartyPageManager manager;
    private Outline borderOutline;
    private bool isHovered;

    public void Setup(Unit unit, bool isActive, PartyPageManager mngr)
    {
        manager = mngr;
        isActiveSlot = isActive;
        assignedUnit = unit;
        isHovered = false;

        if (unitNameText != null)
            unitNameText.text = (unit != null) ? unit.unitName : "EMPTY";
        
        if (levelText != null)
            levelText.text = (unit != null) ? "LVL " + unit.unitLevel : "";

        // Standard setup for interaction
        borderOutline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        borderOutline.effectDistance = new Vector2(3, -3);
        borderOutline.enabled = false;

        Button btn = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => manager.SelectTotem(this));
        
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (manager == null || borderOutline == null) return;

        bool isPendingSwap = manager.IsFirstSelected(this);
        borderOutline.enabled = isPendingSwap || isHovered;
        
        // Match the yellow highlight for selection[cite: 6]
        borderOutline.effectColor = isPendingSwap ? new Color(1f, 0.92f, 0.016f, 1f) : Color.white;
    }

    public void OnSelect(BaseEventData eventData) { isHovered = true; UpdateVisuals(); }
    public void OnDeselect(BaseEventData eventData) { isHovered = false; UpdateVisuals(); }
    public void OnPointerEnter(PointerEventData eventData) { isHovered = true; UpdateVisuals(); }
    public void OnPointerExit(PointerEventData eventData) { isHovered = false; UpdateVisuals(); }
}