using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TMP_Text nameText;
    public TMP_Text mpText;

    public TMP_Text hpText;

    public Image background;

    public void SETHUD(Unit unit)
    {
        nameText.text = unit.unitName;
        mpText.text = "MP: " + unit.currentMP + "/" + unit.maxMP;
        hpText.text = "HP: " + unit.currentHP + "/" + unit.maxHP;
    }

    public void SetHP(int hp, int maxHP)
    {
        hpText.text = "HP: " + hp + "/" + maxHP;
        if (hp <= 0)
        {
            background.color = Color.red;
        }
        else
        {
            background.color = new Color32(116, 116, 116, 255);
        }
    }

    public void SetMP(int mp, int maxMP)
    {
        mpText.text = "MP: " + mp + "/" + maxMP;
    }
}
