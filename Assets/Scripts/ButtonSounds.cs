using UnityEngine;
using UnityEngine.EventSystems;

// This script automatically detects when the EventSystem highlights or clicks this object
public class UIButtonSounds : MonoBehaviour, ISelectHandler, ISubmitHandler, IPointerClickHandler
{
    // Triggered when the keyboard/controller arrows move onto this button
    public void OnSelect(BaseEventData eventData)
    {
        if (UISFXManager.Instance != null)
        {
            UISFXManager.Instance.PlayNav();
        }
    }

    // Triggered when you press Z, Enter, or Space on this button
    public void OnSubmit(BaseEventData eventData)
    {
        if (UISFXManager.Instance != null)
        {
            UISFXManager.Instance.PlaySelect();
        }
    }

    // Triggered when you click it with a mouse
    public void OnPointerClick(PointerEventData eventData)
    {
        if (UISFXManager.Instance != null)
        {
            UISFXManager.Instance.PlaySelect();
        }
    }
}