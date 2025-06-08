using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DankestDungeon.Skills; // For StatType

// If TemporaryModifier is used by other systems, consider moving it to its own file.
[System.Serializable]
public class TemporaryModifier
{
    public StatType statType;
    public float value;
    public int duration; // Turns remaining
    public bool isBuff;
    public string sourceName;
    // Optional: public ModifierApplicationType applicationType;
}
// public enum ModifierApplicationType { Flat, PercentAdd, PercentMult }

public class CharacterBuffs
{
    private List<TemporaryModifier> activeTemporaryModifiers = new List<TemporaryModifier>();
    private string characterName = "Character"; // For logging

    public CharacterBuffs(string ownerName)
    {
        this.characterName = ownerName;
    }

    public void AddModifier(TemporaryModifier modifier)
    {
        activeTemporaryModifiers.Add(modifier);
        Debug.Log($"[BUFFS] {characterName} received modifier: {modifier.statType} {modifier.value} for {modifier.duration} turns from {modifier.sourceName}.");
    }

    public void TickModifiers()
    {
        for (int i = activeTemporaryModifiers.Count - 1; i >= 0; i--)
        {
            activeTemporaryModifiers[i].duration--;
            if (activeTemporaryModifiers[i].duration <= 0)
            {
                Debug.Log($"[BUFFS] {characterName} modifier {activeTemporaryModifiers[i].statType} from {activeTemporaryModifiers[i].sourceName} expired.");
                activeTemporaryModifiers.RemoveAt(i);
            }
        }
    }

    public float GetModifiedStatValue(StatType type, float baseValue)
    {
        float modifiedValue = baseValue;
        // Example: Apply flat modifiers
        modifiedValue += activeTemporaryModifiers.Where(m => m.statType == type /* && m.applicationType == ModifierApplicationType.Flat */).Sum(m => m.value);
        
        // Add other application types (PercentAdd, PercentMult) as needed
        // float percentBonus = activeTemporaryModifiers.Where(m => m.statType == type && m.applicationType == ModifierApplicationType.PercentAdd).Sum(m => m.value);
        // modifiedValue *= (1f + percentBonus);
        return modifiedValue;
    }

    public IReadOnlyList<TemporaryModifier> GetActiveModifiers() => activeTemporaryModifiers;
}