using UnityEngine;
using DankestDungeon.Skills; // Assuming your enums are here

// ... (DamageEffectResult, HealEffectResult etc. structs from above) ...

public class SkillEffectProcessor : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private float baseCritMultiplier = 1.5f;
    [SerializeField] private int minimumDamage = 1;

    // REMOVE the old ProcessEffect method. CombatSystem will call specific calculation methods.
    // public void ProcessEffect(...) { ... }

    public DamageEffectResult CalculateDamageEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || !target.IsAlive || caster == null) // Use IsAlive property
            return new DamageEffectResult { success = false };

        float totalDamage = CalculateDamageValue(caster, target, effectData, rankData, out bool isCrit);
        int finalDamage = Mathf.RoundToInt(totalDamage);
        finalDamage = Mathf.Max(minimumDamage, finalDamage);

        Debug.Log($"[SKILL PROCESSOR] Calculated {finalDamage} damage for {target.GetName()}. Crit: {isCrit}");
        return new DamageEffectResult { finalDamage = finalDamage, isCrit = isCrit, success = true };
    }

    public HealEffectResult CalculateHealEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || caster == null)
            return new HealEffectResult { success = false };
        if (!target.IsAlive && effectData.effectType == SkillEffectType.Heal) // Use IsAlive property
        {
             // You might have specific revive logic later
            // For now, basic heal fails on dead.
            // If it's a revive skill, this logic would be different.
            // return new HealEffectResult { success = false };
        }


        float totalHeal = CalculateHealValue(caster, effectData, rankData, out bool isCrit);
        int finalHeal = Mathf.RoundToInt(totalHeal);

        Debug.Log($"[SKILL PROCESSOR] Calculated {finalHeal} healing for {target.GetName()}. Crit: {isCrit}");
        return new HealEffectResult { finalHeal = finalHeal, isCrit = isCrit, success = true };
    }

    public StatModEffectResult CalculateStatModificationEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData, bool isBuff)
    {
        if (target == null || !target.IsAlive || caster == null) // Use IsAlive property
            return new StatModEffectResult { success = false };

        float modValue = effectData.baseValue;
        if (effectData.scalingStat != StatType.None)
        {
            float statValue = GetCharacterStatValue(caster, effectData.scalingStat);
            modValue += statValue * effectData.scalingMultiplier;
        }
        
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

    public StatusEffectApplicationResult CalculateStatusEffectApplication(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || !target.IsAlive || caster == null) // Use IsAlive property
            return new StatusEffectApplicationResult { success = false };

        // This would involve looking up effectData.statusEffect (if it's an SO)
        Debug.Log($"[SKILL PROCESSOR] Calculated status effect application for {target.GetName()}. Duration: {effectData.duration}");
        return new StatusEffectApplicationResult 
        { 
            // statusEffectToApply = effectData.statusEffect, 
            statusEffectName = "PlaceholderStatusName", // Replace with actual data
            duration = effectData.duration, 
            success = true 
        };
    }

    // Internal calculation helpers remain largely the same (CalculateDamageValue, CalculateHealValue, GetCharacterStatValue)
    // ... (these methods are already in your SkillEffectProcessor.cs) ...
    private float CalculateDamageValue(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData, out bool isCrit)
    {
        float baseDamage = effectData.baseValue;
        float totalDamage = baseDamage;

        if (effectData.scalingStat != StatType.None)
        {
            float statValue = GetCharacterStatValue(caster, effectData.scalingStat);
            totalDamage += statValue * effectData.scalingMultiplier;
        }

        float critChance = GetCharacterStatValue(caster, StatType.CritChance) + rankData.critMod;
        isCrit = Random.value < (critChance / 100f);
        
        if (isCrit)
        {
            totalDamage *= baseCritMultiplier;
        }

        float defense = GetCharacterStatValue(target, StatType.Defense); // Assuming physical damage
        // if skill is magical, use GetMagicResistance()
        totalDamage = Mathf.Max(0, totalDamage - defense);


        return totalDamage;
    }

    private float CalculateHealValue(Character caster, SkillEffectData effectData, SkillRankData rankData, out bool isCrit)
    {
        float baseHeal = effectData.baseValue;
        float totalHeal = baseHeal;

        if (effectData.scalingStat != StatType.None)
        {
            float statValue = GetCharacterStatValue(caster, effectData.scalingStat);
            totalHeal += statValue * effectData.scalingMultiplier;
        }

        float critChance = GetCharacterStatValue(caster, StatType.CritChance) + rankData.critMod;
        isCrit = Random.value < (critChance / 100f);
        
        if (isCrit)
        {
            totalHeal *= baseCritMultiplier;
        }
        return totalHeal;
    }

    private float GetCharacterStatValue(Character character, StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackPower:
                return character.GetAttackPower();
            case StatType.Defense:
                return character.GetDefense();
            case StatType.MagicPower:
                return character.GetMagicPower();
            case StatType.MagicResistance:
                return character.GetMagicResistance();
            case StatType.CritChance:
                return character.GetCritChance();
            case StatType.Speed:
                return character.GetSpeed();
            default:
                Debug.LogWarning($"Stat type {statType} not implemented in GetCharacterStatValue");
                return 0f;
        }
    }
}