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
        [ShowIf("IsElementApplicable")] 
        [EnumToggleButtons]
        public ElementType elementType = ElementType.Physical;

        [Tooltip("Base value for damage, healing, stat change amount, status effect potency, or ranks to move for MoveTarget (integer, -2 to +2).")]
        [ValidateInput("ValidateBaseValueForMoveTarget", "For MoveTarget, Base Value must be an integer between -2 and +2.", IncludeChildren = true)]
        public float baseValue;

        [Tooltip("Which of the caster's stats, if any, does this effect scale with?")]
        [EnumToggleButtons]
        [HideIf("effectType", SkillEffectType.MoveTarget)] // Hide for MoveTarget as it doesn't scale
        public StatType scalingStat = StatType.None;
        
        [Tooltip("Multiplier for the scaling stat's contribution. E.g., 0.5 for 50% of scalingStat.")]
        [ShowIf("ShowScalingMultiplier")] // Updated condition
        public float scalingMultiplier = 1f;

        [Tooltip("Duration in turns for effects like buffs, debuffs, or status effects.")]
        [ShowIf("IsDurationApplicable")]
        [HideIf("effectType", SkillEffectType.MoveTarget)] // Hide for MoveTarget
        public int duration = 1;

        [Tooltip("Which stat is affected by BuffStat or DebuffStat.")]
        [ShowIf("IsStatModificationEffect")]
        [EnumToggleButtons]
        public StatType statToModify = StatType.None;

        [Tooltip("Reference to a StatusEffect ScriptableObject if applying/clearing a status.")]
        [ShowIf("IsStatusEffectRelated")]
        public StatusEffectSO statusEffectToApply;

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

        private bool ShowScalingMultiplier()
        {
            return scalingStat != StatType.None && effectType != SkillEffectType.MoveTarget;
        }

        // Odin Inspector Validation for baseValue when effectType is MoveTarget
        private bool ValidateBaseValueForMoveTarget(float value, ref string errorMessage)
        {
            if (effectType == SkillEffectType.MoveTarget)
            {
                if (value % 1 != 0) // Check if it's not an integer
                {
                    errorMessage = "Base Value must be a whole number (integer) for MoveTarget.";
                    return false;
                }
                int intValue = Mathf.RoundToInt(value);
                if (intValue < -2 || intValue > 2)
                {
                    errorMessage = "Base Value for MoveTarget must be between -2 and +2 (inclusive).";
                    return false;
                }
            }
            return true;
        }
    }
}