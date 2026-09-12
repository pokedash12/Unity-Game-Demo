using System.Linq;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public int unitLevel;
    public int maxHP;
    public int maxMP;
    public int currentMP;
    public int currentHP;

    [Header("Core Stats")]
    public int strength;   // Physical Damage
    public int defense;    // Physical Mitigation
    public int magic;      // Magic Damage & Mitigation
    public int speed;      // Turn Order & Accuracy
    public int luck;       // Crit Chance & Crit Evasion

    [Header("Growth")]
    public int statPointsAvailable; // Points to spend on level up

    // Modified TakeDamage to allow for different damage types
    public bool TakeDamage(int dmg)
    {
        currentHP -= dmg;
        if (currentHP <= 0)
        {
            currentHP = 0;
            return true;
        }
        return false;
    }

    public void LevelUp()
    {
        unitLevel++;
        statPointsAvailable += 2;
        // Optionally increase HP/MP automatically here
        maxHP += 10;
        currentHP = maxHP;
        Debug.Log($"{unitName} leveled up to {unitLevel}! 2 points available.");
    }

    public void AllocateStat(string statName)
    {
        if (statPointsAvailable <= 0) return;

        switch (statName.ToLower())
        {
            case "strength": strength++; break;
            case "defense": defense++; break;
            case "magic": magic++; break;
            case "speed": speed++; break;
            case "luck": luck++; break;
            default: return;
        }
        statPointsAvailable--;
    }

    // --- Keep existing Skill logic below ---
    public Skill[] skills;
    public Skill[] allSkills;
    public char[] weaknesses;
    public char[] resistances;

    public void addSkill(Skill skill)
    {
        if (skill == null) return;
        
        // Check for duplicates to prevent the same skill being added multiple times
        if (allSkills != null && allSkills.Contains(skill)) return;

        // Reassign the array with the new skill appended
        allSkills = (allSkills ?? new Skill[0]).Concat(new Skill[] { skill }).ToArray();
        Debug.Log($"{unitName} learned {skill.skillName}!");
        if(skills.Length < 6)
        {
            skills = skills.Append(skill).ToArray();
        }
    }
    
    public void LoadPlayer(
    string name, int level, int mHP, int mMP, int cHP, int cMP, 
    int str, int def, int mag, int spd, int lck, int points, // New Params
    Skill[] loadedActiveSkills, Skill[] loadedAllSkills, 
    char[] weak, char[] res)
    {
        unitName = name;
        unitLevel = level;
        maxHP = mHP;
        maxMP = mMP;
        currentHP = cHP;
        currentMP = cMP;

        // Apply the new stats
        strength = str;
        defense = def;
        magic = mag;
        speed = spd;
        luck = lck;
        statPointsAvailable = points;

        skills = loadedActiveSkills ?? new Skill[0];
        allSkills = loadedAllSkills ?? new Skill[0];
        weaknesses = weak != null ? (char[])weak.Clone() : new char[0];
        resistances = res != null ? (char[])res.Clone() : new char[0];
    }
}