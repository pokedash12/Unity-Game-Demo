using System;
using System.Linq;
using UnityEngine;

public static class BattleMath
{
    // NEW: Added 'power' parameter. Defaults to 15 for basic attacks.
    public static int CalculateDamage(Unit attacker, Unit defender, bool isMagic, int power = 15)
    {
        float rawDamage;
        if (isMagic)
        {
            // Magic scales with the provided power + Magic stat
            rawDamage = power + (attacker.magic * 1.5f) + (attacker.unitLevel * 2) - (defender.magic * 0.25f);
        }
        else
        {
            // Physical scales with the provided power + Strength stat
            rawDamage = power + (attacker.strength * 1.5f) + (attacker.unitLevel * 2) - (defender.defense * 0.25f);
        }

        // Ensure a minimum damage
        int minDamage = Mathf.Max(1, attacker.unitLevel);
        return Mathf.Max(minDamage, Mathf.RoundToInt(rawDamage)) + Mathf.RoundToInt(UnityEngine.Random.Range(0.0f, 1.0f) * 7);
    }

    public static int CalculateHealing(Unit user, int skillPower)
    {
        // Base healing is skill potency + user's magic stat
        float variance = UnityEngine.Random.Range(0.9f, 1.1f);
        float baseHeal = skillPower + user.magic;

            if (user.resistances.Contains('h'))
            {
                variance *= 1.3f;
            }

        // Apply a slight variance (0.9 to 1.1) so heals aren't always the exact same number
        
        return Mathf.RoundToInt(baseHeal * variance);
    }

    public static bool CheckHit(Unit attacker, Unit defender)
    {
        // NEW: Increased base hit rate to 95%.
        float chance = 0.95f + ((attacker.speed - defender.speed) * 0.01f);
        
        // NEW: Clamped to 88% minimum so missing is rare but still possible
        chance = Mathf.Clamp(chance, 0.80f, 1.0f); 
        return UnityEngine.Random.value <= chance;
    }

    public static bool CheckCrit(Unit attacker, Unit defender)
    {
        // Base 5% crit rate, adjusted by luck difference
        float chance = 0.05f + ((attacker.luck - defender.luck) * 0.01f);
        return UnityEngine.Random.value <= chance;
    }
}