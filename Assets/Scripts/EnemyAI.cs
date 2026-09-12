using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Knowledge { Unknown, Weakness, Resistance, Neutral }

public class EnemyAI : MonoBehaviour
{
    // Tracks [TargetIndex][Element] -> Knowledge
    private Dictionary<int, Dictionary<char, Knowledge>> elementalMemory = new Dictionary<int, Dictionary<char, Knowledge>>();
    
    private int lastTargetIndex = -1;
    private int repeatTargetCount = 0;
    private bool realizedLowMP = false;

    

    public void ResetMemory()
    {
        elementalMemory.Clear();
        lastTargetIndex = -1;
        repeatTargetCount = 0;
        realizedLowMP = false;
    }

    public (Unit target, Skill skill) DetermineAction(Unit self, List<Unit> party)
    {
        // 1. Filter for valid (alive) targets
        List<int> aliveIndices = new List<int>();
        for (int i = 0; i < party.Count; i++)
            if (party[i].currentHP > 0) aliveIndices.Add(i);

        if (aliveIndices.Count == 0) return (null, null);

        bool isPhysicalAttacker = self.strength >= self.magic;
        
        List<Skill> affordableSkills = self.skills.Where(s => s != null && s.mp <= self.currentMP).ToList();
        if (!realizedLowMP && self.skills.Any(s => s.mp > self.currentMP)) {
            Skill test = self.skills[Random.Range(0, self.skills.Length)];
            if (self.currentMP < test.mp) {
                realizedLowMP = true;
                return (null, test); 
            }
        }

        float bestScore = -1f;
        Unit bestTarget = party[aliveIndices[0]];
        Skill bestSkill = null; 

        bool hasSingleTargetOption = affordableSkills.Any(s => !s.multiTarget);

        // A. Evaluate Normal Attack (Single Target)
        foreach (int tIdx in aliveIndices)
        {
            float targetWeight = (tIdx == 0) ? 1.0f : 1.25f;
            if (tIdx == lastTargetIndex) targetWeight *= 0.3f; // Anti-Relentless

            float physicalScore = targetWeight * (isPhysicalAttacker ? 1.5f : 0.8f) * Random.Range(0.8f, 1.2f);
            if (physicalScore > bestScore) {
                bestScore = physicalScore;
                bestTarget = party[tIdx];
                bestSkill = null;
            }
        }

        // B. Evaluate Skills
        foreach (Skill s in affordableSkills)
        {
            if (s.multiTarget)
            {
                // Logic Check: Don't waste AoE on a single survivor if we have single-target moves
                if (aliveIndices.Count == 1 && hasSingleTargetOption) continue;

                float multiScore = 0f;
                
                // Score against ALL alive targets and combine
                foreach(int tIdx in aliveIndices)
                {
                    float weight = (tIdx == 0) ? 1.0f : 1.25f;
                    if (s.element == 'p' || s.element == 'P') weight *= isPhysicalAttacker ? 2.0f : 1.0f;
                    else weight *= !isPhysicalAttacker ? 2.0f : 1.0f;

                    Knowledge k = GetKnowledge(tIdx, s.element);
                    if (k == Knowledge.Weakness) weight *= 3f; 
                    if (k == Knowledge.Resistance) weight *= 0.2f; 
                    
                    multiScore += weight; // Sum the effectiveness
                }

                multiScore *= Random.Range(0.8f, 1.2f) * 0.4f;

                if (multiScore > bestScore)
                {
                    bestScore = multiScore;
                    bestTarget = party[aliveIndices[0]]; // Dummy target, BattleSystem will handle the rest
                    bestSkill = s;
                }
            }
            else
            {
                // Evaluate Single Target Skills normally
                foreach (int tIdx in aliveIndices)
                {
                    float weight = (tIdx == 0) ? 1.0f : 1.25f;
                    if (tIdx == lastTargetIndex) weight *= 0.3f;

                    if (s.element == 'p' || s.element == 'P') weight *= isPhysicalAttacker ? 3.0f : 1.0f;
                    else weight *= !isPhysicalAttacker ? 3.0f : 1.0f;

                    Knowledge k = GetKnowledge(tIdx, s.element);
                    if (k == Knowledge.Weakness) weight *= 3f; 
                    if (k == Knowledge.Resistance) weight *= 0.2f; 

                    weight *= Random.Range(0.8f, 1.2f);

                    if (weight > bestScore)
                    {
                        bestScore = weight;
                        bestTarget = party[tIdx];
                        bestSkill = s;
                    }
                }
            }
        }

        // Update tracking
        if (bestSkill == null || !bestSkill.multiTarget) {
            if (party.IndexOf(bestTarget) == lastTargetIndex) repeatTargetCount++;
            else { lastTargetIndex = party.IndexOf(bestTarget); repeatTargetCount = 0; }
        }

        return (bestTarget, bestSkill);
    }

    public void RecordResult(int targetIndex, char element, bool wasWeak, bool wasResist)
    {
        if (!elementalMemory.ContainsKey(targetIndex))
            elementalMemory[targetIndex] = new Dictionary<char, Knowledge>();

        if (wasWeak) elementalMemory[targetIndex][element] = Knowledge.Weakness;
        else if (wasResist) elementalMemory[targetIndex][element] = Knowledge.Resistance;
        else elementalMemory[targetIndex][element] = Knowledge.Neutral;
    }

    private Knowledge GetKnowledge(int targetIdx, char element)
    {
        if (elementalMemory.ContainsKey(targetIdx) && elementalMemory[targetIdx].ContainsKey(element))
            return elementalMemory[targetIdx][element];
        return Knowledge.Unknown;
    }
}