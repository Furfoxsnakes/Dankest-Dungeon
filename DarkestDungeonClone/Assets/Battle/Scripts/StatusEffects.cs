using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffects : MonoBehaviour
{
    public enum EffectType
    {
        Poison,
        Bleed,
        Stun,
        Burn,
        HealOverTime
    }

    [System.Serializable]
    public class StatusEffect
    {
        public EffectType effectType;
        public float duration;
        public float magnitude;

        public StatusEffect(EffectType type, float duration, float magnitude)
        {
            this.effectType = type;
            this.duration = duration;
            this.magnitude = magnitude;
        }
    }

    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    public void ApplyEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
        StartCoroutine(EffectDuration(effect));
    }

    private IEnumerator EffectDuration(StatusEffect effect)
    {
        yield return new WaitForSeconds(effect.duration);
        activeEffects.Remove(effect);
    }

    public List<StatusEffect> GetActiveEffects()
    {
        return activeEffects;
    }
}