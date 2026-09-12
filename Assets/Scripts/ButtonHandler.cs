using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHandler : MonoBehaviour, ISelectHandler
{

    [SerializeField] private TMP_Text textDisplay;
    [SerializeField] private string textToDisplay = "";
        public void OnSelect(BaseEventData eventData)
        {
            textDisplay.text = textToDisplay;
        }
}
