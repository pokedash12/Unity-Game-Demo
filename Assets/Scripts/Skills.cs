using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public int damage;
    public char element;
    public string description;
    public int minLevel = 1;

    public int mp = 5;

    public bool multiTarget = false;

    public AudioClip sfx;
}