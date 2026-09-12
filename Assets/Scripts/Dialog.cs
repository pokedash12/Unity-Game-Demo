using System.Collections.Generic;
using System.Collections;

using UnityEngine;



[CreateAssetMenu(fileName = "NewDialog", menuName = "Dialog")]
public class Dialog : ScriptableObject
{
    public string signName;
    public string[] dialogLines;
    public float typingSpeed = 0.05f;
    public AudioClip voiceSound;
}
