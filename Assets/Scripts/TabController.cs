using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required for keyboard UI focus
using UnityEngine.InputSystem; // REQUIRED for New Input System

public class TabController : MonoBehaviour
{
    public Image[] tabImages;
    public GameObject[] pages;
    
    public int currentTabIndex = 0;
    private bool isSelectingTab = true;

    void Start()
    {
        // Start by highlighting the first tab, but don't open it yet[cite: 11]
        HighlightTab(0);
        
        // Hide all pages initially[cite: 11]
        for(int i = 0; i < pages.Length; i++) 
        {
            pages[i].SetActive(false);
        }
    }

    void Update()
    {
        // Get the current keyboard state
        var keyboard = Keyboard.current;
        if (keyboard == null) return; 

        if (isSelectingTab)
        {
            // TAB NAVIGATION
            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                currentTabIndex = (currentTabIndex + 1) % tabImages.Length;
                HighlightTab(currentTabIndex);
                UISFXManager.Instance.PlayNav();
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                currentTabIndex--;
                if (currentTabIndex < 0) currentTabIndex = tabImages.Length - 1;
                HighlightTab(currentTabIndex);
                UISFXManager.Instance.PlayNav();
            }
            // OPEN TAB (Z Key)
            else if (keyboard.zKey.wasPressedThisFrame)
            {
                isSelectingTab = false;
                OpenTab(currentTabIndex);
                UISFXManager.Instance.PlaySelect();
            }
        }
        else
        {
            // CLOSE TAB (X Key)
            if (keyboard.xKey.wasPressedThisFrame)
            {
                isSelectingTab = true;
                CloseAllTabs();
                HighlightTab(currentTabIndex);
                EventSystem.current.SetSelectedGameObject(null); 
                UISFXManager.Instance.PlayBack();
            }
        }
    }

    private void HighlightTab(int index)
    {
        for (int i = 0; i < tabImages.Length; i++)
        {
            // Yellow for highlighted, Grey for inactive[cite: 11]
            tabImages[i].color = (i == index) ? Color.yellow : Color.grey; 
        }
    }

    private void OpenTab(int index)
    {
        CloseAllTabs();
        pages[index].SetActive(true);
        tabImages[index].color = Color.white; // White means the tab is currently OPEN[cite: 11]

        // Pass keyboard focus to the first interactive item in the newly opened page
        Selectable firstElement = pages[index].GetComponentInChildren<Selectable>();
        if (firstElement != null)
        {
            EventSystem.current.SetSelectedGameObject(firstElement.gameObject);
        }
    }

    private void CloseAllTabs()
    {
        for(int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(false);
        }
    }
}