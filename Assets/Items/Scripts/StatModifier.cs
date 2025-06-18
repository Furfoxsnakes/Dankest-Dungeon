using System;
using DankestDungeon.Skills; // Assuming StatType is in this namespace or a global one

[Serializable]
public struct StatModifier
{
    public StatType Stat; // Use your existing StatType enum
    public float Value;
    // Optional: public bool IsPercentageModifier; // For more complex modifiers
}