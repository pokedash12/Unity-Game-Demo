using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;

public class sign : MonoBehaviour, IInteractable
{
    public Dialog dialogData;
    public GameObject dialogPanel;
    public TMP_Text dialogueText;

    private float inputDelay = 0.1f;
    private float startTime;

    private int dialogIndex;
    private bool isTyping, isDialogActive;

    public void Interact()
    {
        if(dialogData == null || (PauseController.IsGamePaused && !isDialogActive))
        {
            return;
        }

        if (isDialogActive)
        {
            NextLine();
        }
        else
        {
            StartDialog();
        }
    }

    public bool CanInteract()
    {
        // return whether the player can interact with this sign
        return !isDialogActive && !isTyping;
    }

    void StartDialog()
    {
        isDialogActive = true;
        dialogIndex = 0;

        dialogPanel.SetActive(true);
        PauseController.SetPause(true);
        startTime = Time.unscaledTime;

        StartCoroutine(TypeLine());

    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");
        foreach(char letter in dialogData.dialogLines[dialogIndex])
        {
            dialogueText.text +=letter;
            if (!char.IsWhiteSpace(letter))
            {
                SoundEffectManager.PlayVoice(dialogData.voiceSound);
            }
            yield return new WaitForSecondsRealtime(dialogData.typingSpeed);

        }

        isTyping = false;
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogData.dialogLines[dialogIndex]);
            isTyping = false;
        }

        else if(++dialogIndex < dialogData.dialogLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialog();
        }
    }

    public void EndDialog()
    {
        StopAllCoroutines();
        isDialogActive = false;
        dialogueText.SetText("");
        dialogPanel.SetActive(false);
        PauseController.SetPause(false);
    }

    void Update()
    {
        if (!isDialogActive) return;

        if (Time.unscaledTime - startTime < inputDelay)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.zKey.wasPressedThisFrame)
        {
            NextLine();
        }
    }
}
