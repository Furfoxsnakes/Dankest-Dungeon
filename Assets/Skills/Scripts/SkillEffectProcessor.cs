using UnityEngine;
using DankestDungeon.Skills; // Assuming StatType and other enums are here
using DankestDungeon.Characters; // For ActiveStatusEffect
using DankestDungeon.StatusEffects; // For StatusEffectSO

public class SkillEffectProcessor : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private float baseCritMultiplier = 1.5f;
    [SerializeField] private int minimumDamage = 1;

    private BattleUI battleUI; // Add this field to store the reference

    // ---- Add this Initialize method ----
    public void Initialize(BattleUI ui)
    {
        this.battleUI = ui;
        if (this.battleUI == null)
        {
            Debug.LogError("[SkillEffectProcessor] Initialization with a null BattleUI reference!");
        }
        else
        {
            Debug.Log("[SkillEffectProcessor] Initialized successfully with BattleUI.");
        }
    }
    // ---- End of new method ----

    // This is your existing method that returns DamageEffectResult - KEEP THIS
    public DamageEffectResult CalculateDamageEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || !target.IsAlive || caster == null)
            return new DamageEffectResult { success = false, finalDamage = 0, isCrit = false, elementType = effectData.elementType };

        float totalDamage = CalculateDamageValue(caster, target, effectData, rankData, out bool isCrit);
        int finalDamage = Mathf.RoundToInt(totalDamage);
        finalDamage = Mathf.Max(minimumDamage, finalDamage);

        Debug.Log($"[SKILL PROCESSOR] Calculated {finalDamage} {effectData.elementType} damage for {target.GetName()}. Crit: {isCrit}");
        return new DamageEffectResult { finalDamage = finalDamage, isCrit = isCrit, success = true, elementType = effectData.elementType };
    }

    // REMOVE THE OTHER CalculateDamageEffect method that returns 'DamageResult'
    // public DamageResult CalculateDamageEffect(Character attacker, Character target, SkillEffectData effectData, SkillRankData skillRankData)
    // {
    //     // ... THIS VERSION SHOULD BE DELETED ...
    // }


    public HealEffectResult CalculateHealEffect(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || caster == null)
            return new HealEffectResult { success = false, finalHeal = 0, isCrit = false, elementType = effectData.elementType };
        
        float totalHeal = CalculateHealValue(caster, effectData, rankData, out bool isCrit);
        int finalHeal = Mathf.RoundToInt(totalHeal);

        Debug.Log($"[SKILL PROCESSOR] Calculated {finalHeal} {effectData.elementType} healing for {target.GetName()}. Crit: {isCrit}");
        return new HealEffectResult { finalHeal = finalHeal, isCrit = isCrit, success = true, elementType = effectData.elementType };
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
    
    // New method to calculate the application of a status effect
    public StatusEffectApplicationResult CalculateStatusEffectApplication(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        if (target == null || caster == null || effectData.statusEffectToApply == null)
            return new StatusEffectApplicationResult { success = false };

        bool success = Random.value <= effectData.chance;
        if (!success)
        {
            Debug.Log($"[SKILL PROCESSOR] Status effect '{effectData.statusEffectToApply.statusNameKey}' failed to apply to {target.GetName()} due to chance.");
            return new StatusEffectApplicationResult { success = false, statusEffectName = effectData.statusEffectToApply.statusNameKey };
        }

        // Potency for the status effect (e.g., damage per tick) is calculated here
        // This uses the same scaling as other effects.
        float potency = CalculateValueWithScaling(caster, effectData.baseValue, effectData.scalingStat, effectData.scalingMultiplier);

        Debug.Log($"[SKILL PROCESSOR] Calculated status effect application for {target.GetName()}: '{effectData.statusEffectToApply.statusNameKey}', Duration: {effectData.duration}, Potency: {potency}, Element: {effectData.elementType}");
        return new StatusEffectApplicationResult
        {
            // statusEffectToApply = effectData.statusEffectToApply, // We'll pass effectData directly to Character
            statusEffectName = effectData.statusEffectToApply.statusNameKey,
            duration = effectData.duration,
            elementType = effectData.elementType,
            potency = potency,
            success = true
        };
    }
    
    // New method to process a single tick of an active status effect
    public void ProcessStatusEffectTick(Character target, ActiveStatusEffect activeEffect)
    {
        if (target == null || !target.IsAlive || activeEffect == null) return;

        StatusEffectSO definition = activeEffect.Definition;
        float tickPotency = activeEffect.Potency; // Potency was set at application time

        // Debug.Log($"[SKILL PROCESSOR] Ticking status '{definition.statusNameKey}' on {target.GetName()}. Type: {definition.tickEffectType}, Potency: {tickPotency}");

        switch (definition.tickEffectType)
        {
            case StatusEffectTickType.DamageOverTime:
                int damageAmount = Mathf.Max(minimumDamage, Mathf.RoundToInt(tickPotency));
                // Future: Consider resistances to activeEffect.EffectElementType
                // For now, direct damage.
                // Create a simple DamageEffectResult for display purposes
                DamageEffectResult tickDamageResult = new DamageEffectResult
                {
                    finalDamage = damageAmount,
                    isCrit = false, // Ticks usually don't crit unless specified
                    success = true,
                    elementType = activeEffect.EffectElementType // Use the element from application
                };
                ApplyAndDisplayDamage(target, tickDamageResult); // Re-use existing display logic
                Debug.Log($"[SKILL PROCESSOR] Status '{definition.statusNameKey}' dealt {damageAmount} {activeEffect.EffectElementType} damage to {target.GetName()}.");
                break;

            case StatusEffectTickType.HealOverTime:
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(tickPotency)); // Minimum 1 heal
                HealEffectResult tickHealResult = new HealEffectResult
                {
                    finalHeal = healAmount,
                    isCrit = false,
                    success = true,
                    elementType = activeEffect.EffectElementType // e.g., ElementType.Healing or ElementType.Holy
                };
                ApplyAndDisplayHeal(target, tickHealResult);
                Debug.Log($"[SKILL PROCESSOR] Status '{definition.statusNameKey}' healed {target.GetName()} for {healAmount} {activeEffect.EffectElementType}.");
                break;

            case StatusEffectTickType.StatModification:
                // This type of status effect would typically apply/remove a TemporaryModifier
                // via CharacterBuffs when it's applied and when it expires.
                // Ticking logic for stat mods is less common unless the value changes each turn.
                // For now, we assume stat mods are handled by CharacterBuffs directly or on application/removal of status.
                Debug.LogWarning($"[SKILL PROCESSOR] StatModification tick for '{definition.statusNameKey}' not fully implemented for per-turn changes. Applied/Removed via CharacterBuffs typically.");
                break;
            case StatusEffectTickType.None:
                break;
        }
    }

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

    // ---- Add the ApplyAndDisplayDamage and ApplyAndDisplayHeal methods if they are not already present ----
    // These methods were suggested previously and are needed to actually show the damage numbers.

    public void ApplyAndDisplayDamage(Character target, DamageEffectResult damageResult)
    {
        if (target == null || !damageResult.success)
        {
            Debug.LogWarning($"[SkillEffectProcessor] ApplyAndDisplayDamage: Invalid target or damage result not successful for {target?.GetName()}.");
            return;
        }

        target.TakeDamage(damageResult.finalDamage); // Future: Pass damageResult.elementType here for resistance calculation

        if (battleUI != null)
        {
            BattleUI.DamageNumberType displayType;
            if (damageResult.isCrit)
            {
                // Determine critical display type based on element
                switch (damageResult.elementType)
                {
                    case ElementType.Fire:
                        displayType = BattleUI.DamageNumberType.CriticalFireDamage;
                        break;
                    // Add cases for other elements if they have unique critical popups
                    case ElementType.Physical:
                    default:
                        displayType = BattleUI.DamageNumberType.CriticalDamage;
                        break;
                }
            }
            else
            {
                // Determine normal display type based on element
                switch (damageResult.elementType)
                {
                    case ElementType.Fire:
                        displayType = BattleUI.DamageNumberType.FireDamage;
                        break;
                    // Add cases for other elements
                    case ElementType.Physical:
                    default:
                        displayType = BattleUI.DamageNumberType.NormalDamage;
                        break;
                }
            }
            battleUI.ShowDamageNumber(target, damageResult.finalDamage, displayType);
        }
        else
        {
            Debug.LogError("[SkillEffectProcessor] BattleUI reference is null. Cannot display damage number.");
        }
    }

    public void ApplyAndDisplayHeal(Character target, HealEffectResult healResult)
    {
        if (target == null || !healResult.success)
        {
            Debug.LogWarning($"[SkillEffectProcessor] ApplyAndDisplayHeal: Invalid target or heal result not successful for {target?.GetName()}.");
            return;
        }

        target.HealDamage(healResult.finalHeal); // Future: Pass healResult.elementType for heal effectiveness modifiers

        if (battleUI != null)
        {
            BattleUI.DamageNumberType displayType;
            // Assuming ElementType.Healing for standard heals, or specific types like Holy.
            if (healResult.elementType == ElementType.Healing || healResult.elementType == ElementType.Holy) // Example
            {
                displayType = healResult.isCrit ? BattleUI.DamageNumberType.CriticalHeal : BattleUI.DamageNumberType.Heal;
            }
            else // Fallback or other elemental "heals" (e.g. lifesteal as Shadow)
            {
                displayType = healResult.isCrit ? BattleUI.DamageNumberType.CriticalHeal : BattleUI.DamageNumberType.Heal; // Default heal visuals
                // Or you could have BattleUI.DamageNumberType.ShadowHeal etc.
            }
            battleUI.ShowDamageNumber(target, healResult.finalHeal, displayType);
        }
        else
        {
            Debug.LogError("[SkillEffectProcessor] BattleUI reference is null. Cannot display healing number.");
        }
    }
}