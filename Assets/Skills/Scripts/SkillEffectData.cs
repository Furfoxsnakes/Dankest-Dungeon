using UnityEngine;
using Sirenix.OdinInspector; // If you want to use Odin for these nested classes too
using DankestDungeon.Skills; // Your enums namespace
using DankestDungeon.StatusEffects; // Ensure this using directive is present

namespace DankestDungeon.Skills
{
    [System.Serializable]
    public class SkillEffectData
    {
        [EnumToggleButtons]
        public SkillEffectType effectType;

        [Tooltip("The elemental type of this effect, for Damage, Healing, or Status Effects.")]
        [ShowIf("IsElementApplicable")] // Updated condition
        [EnumToggleButtons]
        public ElementType elementType = ElementType.Physical;

        [Tooltip("Base value for damage, healing, stat change amount, or status effect potency.")]
        public float baseValue;

        [Tooltip("Which of the caster's stats, if any, does this effect scale with?")]
        [EnumToggleButtons]
        public StatType scalingStat = StatType.None;
        
        [Tooltip("Multiplier for the scaling stat's contribution. E.g., 0.5 for 50% of scalingStat.")]
        [ShowIf("scalingStat", StatType.None, true)] // Show only if scalingStat is not None
        public float scalingMultiplier = 1f;

        [Tooltip("Duration in turns for effects like buffs, debuffs, or status effects.")]
        [ShowIf("IsDurationApplicable")]
        public int duration = 1;

        [Tooltip("Which stat is affected by BuffStat or DebuffStat.")]
        [ShowIf("IsStatModificationEffect")]
        [EnumToggleButtons]
        public StatType statToModify = StatType.None;

        [Tooltip("Reference to a StatusEffect ScriptableObject if applying/clearing a status.")]
        [ShowIf("IsStatusEffectRelated")]
        public StatusEffectSO statusEffectToApply; // Ensure this is the correct name

        [Tooltip("Chance for this effect to apply (0.0 to 1.0). 1.0 means 100% chance.")]
        [Range(0f, 1f)]
        public float chance = 1f;

        // Odin Inspector helper properties for conditional display
        private bool IsDurationApplicable()
        {
            return effectType == SkillEffectType.BuffStat || 
                   effectType == SkillEffectType.DebuffStat || 
                   effectType == SkillEffectType.ApplyStatusEffect;
        }

        private bool IsStatModificationEffect()
        {
            return effectType == SkillEffectType.BuffStat || effectType == SkillEffectType.DebuffStat;
        }

        // Updated this helper method
        private bool IsElementApplicable()
        {
            return effectType == SkillEffectType.Damage || 
                   effectType == SkillEffectType.Heal ||
                   effectType == SkillEffectType.ApplyStatusEffect;
        }

        private bool IsStatusEffectRelated()
        {
            return effectType == SkillEffectType.ApplyStatusEffect || effectType == SkillEffectType.ClearStatusEffect;
        }
    }
}