using DankestDungeon.Skills;
using DankestDungeon.StatusEffects;

namespace DankestDungeon.Characters
{
    [System.Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffectSO Definition { get; private set; }
        public Character Caster { get; set; } // Changed to public set
        public ElementType EffectElementType { get; private set; }
        public float Potency { get; set; } // Changed to public set
        public int RemainingDuration { get; set; }
        public int CurrentStacks { get; set; }

        public ActiveStatusEffect(StatusEffectSO definition, Character caster, ElementType elementType, float potency, int duration, int stacks = 1)
        {
            Definition = definition;
            Caster = caster;
            EffectElementType = elementType;
            Potency = potency;
            RemainingDuration = duration;
            CurrentStacks = stacks;
        }
    }
}