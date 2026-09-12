using UnityEngine;

public class SkillDatabase
{
    public static List<Skills> AllSkills = new List<Skills>()
    {
        new Skills { name = "Slash", damage = 20, element = 'P', description = "Weak physical attack." },
        new Skills { name = "Fire", damage = 20, element = 'F', description = "Weak fire attack." },
        new Skills { name = "Ice", damage = 20, element = 'I', description = "Weak ice attack." },
        new Skills { name = "Lightning", damage = 20, element = 'L', description = "Weak lightning attack." },
        new Skills { name = "Earth", damage = 20, element = 'E', description = "Weak earth attack." },
        new Skills { name = "Wind", damage = 20, element = 'W', description = "Weak wind attack." }
    };
}