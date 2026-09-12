using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Item")]
public class Item : ScriptableObject
{
    public int ID;

    public string itemName;

    public char effect = 'h';

    public int potency = 10;
    public string description;

    public AudioClip sfx;

}