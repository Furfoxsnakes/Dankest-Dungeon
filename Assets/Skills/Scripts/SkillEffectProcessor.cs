using UnityEngine;
using DankestDungeon.Skills; // Assuming StatType and other enums are here
using DankestDungeon.StatusEffects; // For StatusEffectSO, ActiveStatusEffect, StatusEffectTickType
using DankestDungeon.Characters;   // For Character, StatType, TemporaryModifier

// It's good practice to include using directives for types used in public method signatures
// if they are in different namespaces, though BattleUI might be in the global namespace or a common one.
// using YourNamespace.UI; // If BattleUI is in a specific namespace

public class SkillEffectProcessor : MonoBehaviour
{
    private BattleUI _battleUI;

    [Header("Global Settings")]
    [SerializeField] private float baseCritMultiplier = 1.5f; // Make sure this is used by your CalculateDamageValue
    [SerializeField] private int minimumDamage = 1; // Ensure this is defined

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

    // ---- Add the ApplyAndDisplayDamage and ApplyAndDisplayHeal methods if they are not already present ----
    // These methods were suggested previously and are needed to actually show the damage numbers.

    public void ApplyAndDisplayDamage(Character target, DamageEffectResult damageResult)
    {
        if (target == null || !damageResult.success)
        {
            Debug.LogWarning($"[SkillEffectProcessor] ApplyAndDisplayDamage: Invalid target or damage result not successful for {target?.GetName()}.");
            return;
        }

        target.TakeDamage(damageResult.finalDamage); 

        if (_battleUI != null)
        {
            BattleUI.DamageNumberType type = damageResult.isCrit ? BattleUI.DamageNumberType.CriticalDamage : BattleUI.DamageNumberType.NormalDamage;
            _battleUI.ShowDamageNumber(target, damageResult.finalDamage, type);
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

        target.HealDamage(healResult.finalHeal);

        if (_battleUI != null)
        {
            BattleUI.DamageNumberType type = BattleUI.DamageNumberType.Heal;
            // If you add a specific CriticalHeal type:
            // BattleUI.DamageNumberType type = healResult.isCrit ? BattleUI.DamageNumberType.CriticalHeal : BattleUI.DamageNumberType.Heal;
            _battleUI.ShowDamageNumber(target, healResult.finalHeal, type);
        }
        else
        {
            Debug.LogError("[SkillEffectProcessor] BattleUI reference is null. Cannot display healing number.");
        }
    }

    public void ProcessStatusEffectTick(Character target, ActiveStatusEffect statusEffect)
    {
        if (target == null || !target.IsAlive || statusEffect == null || statusEffect.Definition == null)
        {
            Debug.LogWarning($"[SkillEffectProcessor] ProcessStatusEffectTick: Invalid arguments. Target: {target?.GetName()}, Status: {statusEffect?.Definition?.statusNameKey}");
            return;
        }

        StatusEffectSO definition = statusEffect.Definition;
        float tickPotency = statusEffect.Potency; // This is the calculated per-tick magnitude
        ElementType elementType = statusEffect.EffectElementType; // Element of the status effect's tick.

        switch (definition.tickEffectType)
        {
            case StatusEffectTickType.DamageOverTime:
                int damageToApply = Mathf.Max(minimumDamage, Mathf.RoundToInt(tickPotency));
                
                // Note: Defense/Resistance is typically applied when the DoT potency is initially calculated
                // or it's assumed the tickPotency already accounts for it if it's 'true' damage.
                // If DoT ticks should be re-evaluated against current defenses each tick, that logic would go here.
                // For now, we assume tickPotency is the final damage for the tick.
                
                target.TakeDamage(damageToApply);
                Debug.Log($"[SKILL PROCESSOR] Status '{definition.statusNameKey}' (Tick) dealt {damageToApply} {elementType} damage to {target.GetName()}. Potency: {tickPotency}");

                if (_battleUI != null)
                {
                    // Consider a specific BattleUI.DamageNumberType for status effect damage if desired
                    _battleUI.ShowDamageNumber(target, damageToApply, BattleUI.DamageNumberType.NormalDamage); // Or a new type e.g., StatusEffectDamage
                }
                break;

            case StatusEffectTickType.HealOverTime:
                int healToApply = Mathf.Max(0, Mathf.RoundToInt(tickPotency)); // Healing shouldn't be negative.
                target.HealDamage(healToApply);
                Debug.Log($"[SKILL PROCESSOR] Status '{definition.statusNameKey}' (Tick) healed {healToApply} HP for {target.GetName()}. Potency: {tickPotency}");

                if (_battleUI != null)
                {
                    // Consider a specific BattleUI.DamageNumberType for status effect healing
                    _battleUI.ShowDamageNumber(target, healToApply, BattleUI.DamageNumberType.Heal); // Or a new type e.g., StatusEffectHeal
                }
                break;

            case StatusEffectTickType.StatModification:
                // This assumes the "tick" for a stat modification means applying a temporary modifier
                // whose magnitude is defined by statusEffect.Potency (calculated from the skill)
                // or fallback to definition.statModValue. The duration of this tick-applied modifier
                // should be short (e.g., 1 turn) as it's part of a "tick".
                // If the stat mod is meant to be persistent for the status duration, that should be handled
                // on application/removal of the status effect itself, not per-tick here.

                StatType statToMod = definition.statToModify;
                float modValue = tickPotency; // Primary: use potency from skill application

                // Fallback if Potency was 0 and statModValue is set (though Potency should ideally be calculated correctly)
                if (Mathf.Approximately(modValue, 0) && !Mathf.Approximately(definition.statModValue, 0))
                {
                    modValue = definition.statModValue;
                }

                if (statToMod != StatType.None && !Mathf.Approximately(modValue, 0))
                {
                    TemporaryModifier tickModifier = new TemporaryModifier
                    {
                        statType = statToMod,
                        value = modValue,
                        duration = 1, // Lasts until the next tick cycle, effectively. CharacterBuffs will manage it.
                        isBuff = definition.isBuff, // Or determine from modValue > 0
                        sourceName = $"{definition.statusNameKey} (Tick)"
                    };
                    target.AddTemporaryModifier(tickModifier);
                    Debug.Log($"[SkillEffectProcessor] Status '{definition.statusNameKey}' (Tick) applied temporary modifier to {statToMod} by {modValue} for 1 duration to {target.GetName()}.");
                }
                else
                {
                    // Debug.Log($"[SkillEffectProcessor] Status '{definition.statusNameKey}' (Tick) StatModification: No valid stat/value to modify. Stat: {statToMod}, Value: {modValue}");
                }
                break;

            case StatusEffectTickType.None:
            default:
                // No direct effect this tick. Duration will still be managed by Character.cs.
                // Debug.Log($"[SkillEffectProcessor] Status '{definition.statusNameKey}' on {target.GetName()} ticked with type '{definition.tickEffectType}'. No direct effect processed by tick.");
                break;
        }
    }

    public void Initialize(BattleUI battleUI)
    {
        _battleUI = battleUI;
        // Add any other initialization logic here
        Debug.Log("[SkillEffectProcessor] Initialized with BattleUI.");
    }
}