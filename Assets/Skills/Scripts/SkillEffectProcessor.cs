using UnityEngine;
using DankestDungeon.Skills; // Assuming StatType and other enums are here

public class SkillEffectProcessor : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private float baseCritMultiplier = 1.5f; // Make sure this is used by your CalculateDamageValue
    [SerializeField] private int minimumDamage = 1; // Make sure this is used

    // This is your existing method that returns DamageEffectResult - KEEP THIS
    public DamageEffectResult CalculateDamageEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || !target.IsAlive || caster == null)
            return new DamageEffectResult { success = false, finalDamage = 0, isCrit = false };

        // Use your internal CalculateDamageValue method
        float totalDamage = CalculateDamageValue(caster, target, effectData, rankData, out bool isCrit);
        int finalDamage = Mathf.RoundToInt(totalDamage);
        finalDamage = Mathf.Max(minimumDamage, finalDamage); // Apply minimum damage

        Debug.Log($"[SKILL PROCESSOR] Calculated {finalDamage} damage for {target.GetName()}. Crit: {isCrit}");
        return new DamageEffectResult { finalDamage = finalDamage, isCrit = isCrit, success = true };
    }

    // REMOVE THE OTHER CalculateDamageEffect method that returns 'DamageResult'
    // public DamageResult CalculateDamageEffect(Character attacker, Character target, SkillEffectData effectData, SkillRankData skillRankData)
    // {
    //     // ... THIS VERSION SHOULD BE DELETED ...
    // }


    public HealEffectResult CalculateHealEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || caster == null)
            return new HealEffectResult { success = false, finalHeal = 0, isCrit = false };
        
        // Allow healing on dead targets if it's a revive skill, otherwise, it might not make sense
        // or the HealDamage method on Character handles revival.
        // For now, let CalculateHealValue proceed, Character.HealDamage can handle IsAlive checks.

        float totalHeal = CalculateHealValue(caster, effectData, rankData, out bool isCrit); // Ensure this method exists and is correct
        int finalHeal = Mathf.RoundToInt(totalHeal);

        Debug.Log($"[SKILL PROCESSOR] Calculated {finalHeal} healing for {target.GetName()}. Crit: {isCrit}");
        return new HealEffectResult { finalHeal = finalHeal, isCrit = isCrit, success = true };
    }

    public StatModEffectResult CalculateStatModificationEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData, bool isBuff)
    {
        if (target == null || !target.IsAlive || caster == null) 
            return new StatModEffectResult { success = false };

        float modValue = CalculateValueWithScaling(caster, effectData.baseValue, effectData.scalingStat, effectData.scalingMultiplier);
        
        Debug.Log($"[SKILL PROCESSOR] Calculated stat mod for {target.GetName()}: {effectData.statToModify} by {modValue}, Duration: {effectData.duration}, IsBuff: {isBuff}");
        return new StatModEffectResult
        {
            statToModify = effectData.statToModify,
            modValue = modValue,
            duration = effectData.duration,
            isBuff = isBuff,
            success = true
        };
    }
    
    // Ensure this method is present and correct
    public float CalculateValueWithScaling(Character caster, float baseValue, StatType scalingStat, float scalingMultiplier)
    {
        float finalValue = baseValue;
        if (scalingStat != StatType.None && caster != null)
        {
            float statValue = GetCharacterStatValue(caster, scalingStat); // Use your existing GetCharacterStatValue
            finalValue += statValue * scalingMultiplier;
        }
        return finalValue;
    }

    // Your internal calculation helpers:
    private float CalculateDamageValue(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData, out bool isCrit)
    {
        float totalDamage = CalculateValueWithScaling(caster, effectData.baseValue, effectData.scalingStat, effectData.scalingMultiplier);

        // Crit Chance
        // Assuming GetCharacterStatValue for CritChance returns the base stat (e.g., 5 for 5%)
        // And rankData.critMod is also a whole number percentage modifier
        float critChanceStat = GetCharacterStatValue(caster, StatType.CritChance); 
        float totalCritChance = critChanceStat + rankData.critMod; // e.g. 5 + 10 = 15%
        isCrit = Random.value < (totalCritChance / 100f); // Convert to 0.0 - 1.0 range
        
        if (isCrit)
        {
            // Use criticalDamageMultiplier from CharacterStats, assuming it's a percentage like 150 for 1.5x
            totalDamage *= (caster.Stats.criticalDamageMultiplier / 100f); 
        }

        // Defense Mitigation
        // Determine if physical or magical defense applies. This might need a field in SkillEffectData or SkillDefinitionSO
        // For now, assuming physical damage if effectType is Damage, otherwise needs more info.
        float defense = 0;
        if (effectData.effectType == SkillEffectType.Damage) // Simplistic check, you might have specific damage types
        {
             defense = GetCharacterStatValue(target, StatType.Defense);
        }
        // else if (effectData.damageType == DamageType.Magic) // Example
        // {
        //    defense = GetCharacterStatValue(target, StatType.MagicResistance);
        // }
        
        totalDamage = Mathf.Max(0, totalDamage - defense); // Ensure damage doesn't go below 0 before minimumDamage application

        return totalDamage;
    }

    private float CalculateHealValue(Character caster, SkillEffectData effectData, SkillRankData rankData, out bool isCrit)
    {
        float totalHeal = CalculateValueWithScaling(caster, effectData.baseValue, effectData.scalingStat, effectData.scalingMultiplier);

        float critChanceStat = GetCharacterStatValue(caster, StatType.CritChance);
        float totalCritChance = critChanceStat + rankData.critMod;
        isCrit = Random.value < (totalCritChance / 100f); 
        
        if (isCrit)
        {
            totalHeal *= baseCritMultiplier; // Use the global baseCritMultiplier for heals, or a specific one if desired
        }
        return totalHeal;
    }

    private float GetCharacterStatValue(Character character, StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackPower: return character.GetAttackPower();
            case StatType.Defense: return character.GetDefense();
            case StatType.MagicPower: return character.GetMagicPower();
            case StatType.MagicResistance: return character.GetMagicResistance();
            // GetCritChance in Character.cs already returns a 0-1 float if Stats.criticalChance is divided by 100f
            // If GetCritChance returns a whole number (e.g., 5 for 5%), then it's fine here.
            // Let's assume Character.GetCritChance() returns the value ready for percentage calculation (e.g. 5 for 5%)
            case StatType.CritChance: return character.Stats.criticalChance; // Direct access if GetCritChance() in Character.cs already applies modifiers
            case StatType.Speed: return character.GetSpeed();
            case StatType.MaxHealth: return character.GetMaxHealth();
            default:
                Debug.LogWarning($"Stat type {statType} not implemented in GetCharacterStatValue");
                return 0f;
        }
    }
}