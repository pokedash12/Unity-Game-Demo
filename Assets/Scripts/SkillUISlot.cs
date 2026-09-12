using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Added IPointer interfaces to support mouse hovering
public class SkillUISlot : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text skillNameText;
    public Skill assignedSkill;
    public bool isActiveSlot;
    
    private StatsPageManager manager;
    private Outline borderOutline;
    private bool isHovered;

    public void Setup(Skill skill, bool isActive, StatsPageManager mngr)
    {
        manager = mngr;
        isActiveSlot = isActive;
        assignedSkill = skill;
        isHovered = false;

        if (skillNameText != null)
        {
            skillNameText.text = (skill != null) ? skill.skillName : "---";
            if (manager.customFont != null) skillNameText.font = manager.customFont;
            skillNameText.fontSize = manager.customFontSize;

            if (skill != null)
                skillNameText.color = GetElementColor(skill.element);
            else
                skillNameText.color = Color.white;
        }

        if (GetComponent<Image>() == null)
        {
            Image img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); 
            img.raycastTarget = true;
        }

        borderOutline = GetComponent<Outline>();
        if (borderOutline == null)
        {
            borderOutline = gameObject.AddComponent<Outline>();
            borderOutline.effectDistance = new Vector2(3, -3);
        }
        borderOutline.enabled = false;

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // Optional: prevents Unity's default tint from overriding your colors
            btn.transition = Selectable.Transition.None; 
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnZPressed);
        }
        
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (manager == null || borderOutline == null) return;

        bool isPendingSwap = manager.IsFirstSelected(this);
        bool swapModeActive = manager.IsSwapInProgress();

        borderOutline.enabled = isPendingSwap || isHovered;
        
        if (isPendingSwap || (swapModeActive && isHovered))
        {
            borderOutline.effectColor = new Color(1f, 0.92f, 0.016f, 1f); // Yellow
        }
        else
        {
            borderOutline.effectColor = Color.white;
        }
    }

    private Color GetElementColor(char element)
    {
        switch (char.ToUpper(element))
        {
            case 'P': return Color.gray;
            case 'F': return Color.red;
            case 'I': return new Color(0.6f, 0.8f, 1f);
            case 'L': return Color.yellow;
            case 'E': return new Color(0.45f, 0.25f, 0.05f);
            case 'H': return new Color(1f, 0.5f, 0.8f);
            case 'W': return new Color(0.6f, 1f, 0.6f);
            default: return Color.white;
        }
    }

    public void OnZPressed() => manager.SelectSkill(this);

    // Keyboard/Gamepad selection
    public void OnSelect(BaseEventData eventData) { isHovered = true; UpdateVisuals(); }
    public void OnDeselect(BaseEventData eventData) { isHovered = false; UpdateVisuals(); }

    // Mouse selection (fixes the border issue)
    public void OnPointerEnter(PointerEventData eventData) { isHovered = true; UpdateVisuals(); }
    public void OnPointerExit(PointerEventData eventData) { isHovered = false; UpdateVisuals(); }
}