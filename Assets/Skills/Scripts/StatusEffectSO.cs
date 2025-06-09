using UnityEngine;
using DankestDungeon.Skills; // For ElementType, StatType etc.
using Sirenix.OdinInspector;

namespace DankestDungeon.StatusEffects
{
    public enum StatusEffectTickType
    {
        None,
        DamageOverTime,
        HealOverTime,
        StatModification // For ongoing stat changes tied to the status
    }

    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Dankest Dungeon/Status Effects/Status Effect Definition")]
    public class StatusEffectSO : ScriptableObject
    {
        [BoxGroup("General Info", CenterLabel = true)]
        public string statusId; // Unique ID, can be auto-generated or set manually
        [BoxGroup("General Info")]
        public string statusNameKey; // For localization
        [BoxGroup("General Info")]
        [TextArea(2,4)]
        public string descriptionKey; // For localization

        [BoxGroup("Visuals", CenterLabel = true)]
        [PreviewField(75)]
        public Sprite icon; // Icon to display on UI

        [BoxGroup("Mechanics", CenterLabel = true)]
        [EnumToggleButtons]
        public StatusEffectTickType tickEffectType = StatusEffectTickType.None;

        [BoxGroup("Mechanics")]
        [Tooltip("Is this effect generally considered a buff (positive) or debuff (negative)?")]
        public bool isBuff = false;

        [BoxGroup("Mechanics")]
        [Tooltip("Can multiple instances of this status effect stack on a single target?")]
        public bool canStack = false;
        [ShowIf("canStack")]
        public int maxStacks = 1;


        [BoxGroup("Tick Behavior (Damage/Heal Over Time)", CenterLabel = true)]
        [ShowIf("IsDamageOrHealTick")]
        [Tooltip("Base damage/healing value per tick. Potency from skill application can modify this.")]
        public float baseTickValue = 5f;

        // If the tick itself scales with something (e.g. caster's remaining magic power each turn)
        // For now, we'll assume potency is fixed at application time.

        [BoxGroup("Tick Behavior (Stat Modification)", CenterLabel = true)]
        [ShowIf("IsStatModificationTick")]
        public StatType statToModify = StatType.None;
        [ShowIf("IsStatModificationTick")]
        [Tooltip("Fixed value for stat modification per tick/while active. Can be negative for debuffs.")]
        public float statModValue = 0;


        // Odin Inspector helper
        private bool IsDamageOrHealTick() => tickEffectType == StatusEffectTickType.DamageOverTime || tickEffectType == StatusEffectTickType.HealOverTime;
        private bool IsStatModificationTick() => tickEffectType == StatusEffectTickType.StatModification;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                statusId = System.Guid.NewGuid().ToString();
            }
        }
    }
}