using UnityEngine;
using System;
using DankestDungeon.Skills; // Assuming your enums are here
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using DankestDungeon.Characters; // For ActiveStatusEffect
using DankestDungeon.StatusEffects; // For StatusEffectSO

public class Character : MonoBehaviour
{
    [SerializeField] private CharacterStats stats;
    public CharacterStats Stats => stats;

    [SerializeField] public Animator characterAnimator;
    [SerializeField] private bool debugStateTransitions = false;

    internal int _currentHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;
    public int FormationPosition { get; set; }

    private StateMachine<CharacterState> stateMachine;
    private AnimationType currentAnimationType = AnimationType.None;
    private Action _onAnimationCompleteCallback;

    // === MODULES ===
    [BoxGroup("Modules", CenterLabel = true)]
    [PropertyOrder(5)]
    [SerializeField, InlineProperty, HideLabel] // InlineProperty makes it look integrated
    private CharacterSkills characterSkills = new CharacterSkills();
    public CharacterSkills Skills => characterSkills; // Public accessor

    private CharacterBuffs characterBuffs;
    public CharacterBuffs Buffs => characterBuffs;

    private List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();
    public IReadOnlyList<ActiveStatusEffect> ActiveStatusEffects => activeStatusEffects.AsReadOnly();


    public string GetName()
    {
        return stats != null && !string.IsNullOrEmpty(stats.characterName) ? stats.characterName : gameObject.name;
    }

    public IState GetCurrentState() => stateMachine?.CurrentState;

    // === ODIN INSPECTOR QUICK ACTIONS (moved some to CharacterSkills) ===
    [BoxGroup("Character Quick Actions", CenterLabel = true)]
    [PropertyOrder(100)]
    [Button("Load Default Skills (Character-Specific)"), GUIColor(0.8f, 0.8f, 1f)]
    private void LoadDefaultSkills()
    {
        // This method would typically involve characterStats or a specific config
        // to know which skills are "default" for this character type/class.
        // For example:
        // if (stats.defaultSkills != null) {
        //     foreach(var skillInfo in stats.defaultSkills) {
        //         Skills.LearnSkill(skillInfo.skill, skillInfo.rank);
        //     }
        // }
        Debug.Log($"LoadDefaultSkills called for {GetName()}. Implement character-specific default skills here. Skills can be added via Character.Skills.LearnSkill().");
    }

    [BoxGroup("Character Quick Actions")]
    [PropertyOrder(101)]
    [Button("Export Skills (Console)"), GUIColor(0.8f, 1f, 0.8f)]
    private void ExportSkillsToConsole()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Skills.ExportSkillsToString(sb); // Delegate to CharacterSkills
        Debug.Log(sb.ToString());
    }


    // === PUBLIC SKILL ACCESSORS (delegating to CharacterSkills) ===
    public IReadOnlyDictionary<SkillDefinitionSO, int> LearnedSkills => Skills.LearnedSkills;
    public void LearnSkill(SkillDefinitionSO skill, int rank = 1) => Skills.LearnSkill(skill, rank);
    public void ForgetSkill(SkillDefinitionSO skill) => Skills.ForgetSkill(skill);
    public bool KnowsSkill(SkillDefinitionSO skill) => Skills.KnowsSkill(skill);
    public int GetSkillRank(SkillDefinitionSO skill) => Skills.GetSkillRank(skill);
    public SkillRankData GetSkillRankData(SkillDefinitionSO skill) => Skills.GetSkillRankData(skill);
    public List<SkillDefinitionSO> GetAvailableSkills() => Skills.GetAvailableSkills();


    protected virtual void Awake()
    {
        if (characterAnimator == null) characterAnimator = GetComponent<Animator>();
        
        stateMachine = new StateMachine<CharacterState>();
        if (debugStateTransitions)
        {
            stateMachine.OnStateChanged += (oldState, newState) =>
                Debug.Log($"{GetName()}: {oldState?.GetType().Name} -> {newState?.GetType().Name}");
        }

        if (stats == null) Debug.LogError($"CharacterStats not assigned for {GetName()}!");
        else _currentHealth = stats.maxHealth;

        // Initialize Modules
        characterSkills.Initialize(GetName()); // Pass character name for logging
        characterBuffs = new CharacterBuffs(GetName());
        activeStatusEffects = new List<ActiveStatusEffect>(); // Initialize the list
    }

    protected virtual void Start()
    {
        stateMachine.Initialize(new IdleState(this));
    }

    protected virtual void Update()
    {
        stateMachine.Update();
        if (stateMachine.CheckStateCompletion(out object result) && result is CharacterEvent characterEvent)
        {
            HandleStateEvent(characterEvent);
            stateMachine.CurrentState?.ResetCompletionStatus();
        }
    }

    private void HandleStateEvent(CharacterEvent characterEvent)
    {
        // Debug.Log($"<color=lightblue>[CHARACTER] {GetName()} handling CharacterEvent: {characterEvent}. IsAlive: {IsAlive}</color>");
        switch (characterEvent)
        {
            case CharacterEvent.AttackComplete:
            case CharacterEvent.HitComplete:
            case CharacterEvent.DefendComplete:
            case CharacterEvent.MagicComplete:
            case CharacterEvent.ItemComplete:
                if (IsAlive && !(stateMachine.CurrentState is DeathState))
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                break;
            case CharacterEvent.HitKilled:
                // Debug.Log($"[CHARACTER] {GetName()} was killed by a hit. DeathState should be active or imminently active.");
                break;
            case CharacterEvent.DeathComplete:
                // Debug.Log($"[CHARACTER] {GetName()} death sequence fully complete. Remains in DeathState.");
                break;
            case CharacterEvent.None:
                break;
            default:
                Debug.LogWarning($"[CHARACTER] Unhandled CharacterEvent: {characterEvent} for {GetName()}");
                if (IsAlive && !(stateMachine.CurrentState is IdleState) && !(stateMachine.CurrentState is DeathState))
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                break;
        }
    }

    private Action _internalAnimationCompleteCallback; // For Character's own state machine
    private Action _externalAnimationCompleteCallback; // For ActionSequenceHandler

    // Called by Character's internal states (e.g., AttackState, CastState, HitState)
    public void RegisterInternalAnimationCallback(Action newCallback)
    {
        _internalAnimationCompleteCallback = newCallback;
    }

    public void ClearInternalAnimationCallback()
    {
        _internalAnimationCompleteCallback = null;
    }

    // Called by ActionSequenceHandler
    public void RegisterExternalAnimationCallback(Action newCallback)
    {
        _externalAnimationCompleteCallback = newCallback;
    }

    // Called by CharacterVisuals (or directly by animation events if CharacterVisuals is just a proxy)
    public void OnAnimationComplete()
    {
        // Prioritize invoking the external callback if one is registered,
        // as ActionSequenceHandler is likely driving the sequence.
        // Then clear it so it's a one-shot for ASH.
        Action externalCb = _externalAnimationCompleteCallback;
        if (externalCb != null)
        {
            _externalAnimationCompleteCallback = null; // Clear it immediately
            Debug.Log($"[{gameObject.name}] Invoking EXTERNAL animation complete callback.");
            externalCb.Invoke();
            // Do NOT invoke internal callback if external was present and handled it.
            // The external system (ASH) is now responsible for the flow.
            // If the internal state also needs to react, it should be after ASH is done with this step.
            // This might require ASH to signal the character state machine if needed.
            // For now, let's assume ASH's callback is sufficient for this specific event.
        }
        else
        {
            // If no external callback, then it's for the character's internal state machine.
            Action internalCb = _internalAnimationCompleteCallback;
            if (internalCb != null)
            {
                // Internal callback should also typically be one-shot for a given state's animation.
                // The state itself should re-register if it needs another one.
                _internalAnimationCompleteCallback = null; 
                Debug.Log($"[{gameObject.name}] Invoking INTERNAL animation complete callback.");
                internalCb.Invoke();
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] OnAnimationComplete called, but no internal or external callback was registered.");
            }
        }
    }

    // Modify Character's state machine (e.g., AttackState.Enter) to use RegisterInternalAnimationCallback
    // Example in AttackState.cs:
    // public override void Enter() {
    //    _character.RegisterInternalAnimationCallback(() => {
    //        _character.HandleStateEvent(CharacterEvent.ActionComplete);
    //    });
    //    _character.Visuals.PlayAnimation(AnimationType.Attack, _target);
    // }

    // Character's PlayAnimation, Attack, TakeHit methods should trigger animations
    // but NOT necessarily register callbacks themselves. The state (AttackState, HitState)
    // or external systems (ASH) should register the callbacks they need.

    public void PlayAnimation(AnimationType animType, Character targetForAnim = null)
    {
        // This method should primarily tell CharacterVisuals to play the animation.
        // Callback registration should be done by the caller (ASH or Character's current state).
        Debug.Log($"[{gameObject.name}] PlayAnimation called: {animType}");
        PlayAnimation(animType, targetForAnim);
    }

    public void Attack(Character target)
    {
        // This method should primarily tell CharacterVisuals to play the attack animation.
        // The AttackState should have registered the internal callback.
        Debug.Log($"[{gameObject.name}] Attack called on {target?.name ?? "null target"}");
        PlayAnimation(AnimationType.Attack, target); // Or a specific attack animation
    }

    public void TakeHit()
    {
        // This method should primarily tell CharacterVisuals to play the hit animation.
        // The HitState (if you have one) or ASH should register the callback.
        Debug.Log($"[{gameObject.name}] TakeHit called");
        PlayAnimation(AnimationType.Hit);
    }


    // --- Stat Getters (incorporating temporary modifiers via CharacterBuffs) ---
    public int GetAttackPower() => Mathf.RoundToInt(characterBuffs.GetModifiedStatValue(StatType.AttackPower, Stats.attackPower));
    public int GetDefense() => Mathf.RoundToInt(characterBuffs.GetModifiedStatValue(StatType.Defense, Stats.defense));
    public int GetMagicPower() => Mathf.RoundToInt(characterBuffs.GetModifiedStatValue(StatType.MagicPower, Stats.magicPower));
    public int GetMagicResistance() => Mathf.RoundToInt(characterBuffs.GetModifiedStatValue(StatType.MagicResistance, Stats.magicResistance));
    public float GetCritChance() => characterBuffs.GetModifiedStatValue(StatType.CritChance, Stats.criticalChance / 100f);
    public int GetSpeed() => Mathf.RoundToInt(characterBuffs.GetModifiedStatValue(StatType.Speed, Stats.speed));
    public int GetMaxHealth() => Mathf.RoundToInt(characterBuffs.GetModifiedStatValue(StatType.MaxHealth, Stats.maxHealth));

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        int oldHealth = _currentHealth;
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        Debug.Log($"[CHARACTER] {GetName()} received {amount} damage. Health: {oldHealth} -> {_currentHealth}");
        if (_currentHealth <= 0 && oldHealth > 0) Die();
    }

    public void HealDamage(int amount)
    {
        if (amount <= 0) return;
        int oldHealth = _currentHealth;
        _currentHealth = Mathf.Min(_currentHealth + amount, GetMaxHealth());
        Debug.Log($"[CHARACTER] {GetName()} healed {amount}. Health: {oldHealth} -> {_currentHealth}");
        if (oldHealth <= 0 && _currentHealth > 0 && stateMachine.CurrentState is DeathState)
        {
            Debug.Log($"[CHARACTER] {GetName()} revived! Transitioning to IdleState.");
            stateMachine.ChangeState(new IdleState(this));
        }
    }
    
    public void ApplyDefenseStance()
    {
        if (!IsAlive) return;
        characterBuffs.AddModifier(new TemporaryModifier { 
            statType = StatType.Defense, 
            value = Stats.defense * 0.5f, // Example: 50% defense buff
            duration = 1, 
            isBuff = true,
            sourceName = "Defend Action"
        });
        Debug.Log($"[CHARACTER] {GetName()} applied defense stance.");
    }

    // --- BUFF/DEBUFF MANAGEMENT (delegating to CharacterBuffs) ---
    public void AddTemporaryModifier(TemporaryModifier modifier)
    {
        if (!IsAlive && !(stateMachine.CurrentState is DeathState && (modifier.statType == StatType.MaxHealth || modifier.statType == StatType.Health)))
        {
            Debug.LogWarning($"[CHARACTER] Attempted to add modifier '{modifier.sourceName}' to {GetName()} but character is dead/unsuitable. Not applied.");
            return;
        }
        characterBuffs.AddModifier(modifier);
    }

    public void TickTemporaryModifiers() => characterBuffs.TickModifiers();

    // New methods for Status Effects
    public void ApplyStatusEffect(SkillEffectData effectData, Character caster, float calculatedPotency)
    {
        if (effectData.statusEffectToApply == null)
        {
            Debug.LogWarning($"[Character] Attempted to apply a null status effect to {GetName()}.");
            return;
        }

        // Handle stacking
        ActiveStatusEffect existingEffect = activeStatusEffects.FirstOrDefault(se => se.Definition == effectData.statusEffectToApply);
        if (existingEffect != null)
        {
            if (effectData.statusEffectToApply.canStack)
            {
                existingEffect.CurrentStacks = Mathf.Min(existingEffect.CurrentStacks + 1, effectData.statusEffectToApply.maxStacks);
                existingEffect.RemainingDuration = effectData.duration; // Refresh duration on stack
                existingEffect.Potency = calculatedPotency; // Potentially refresh potency
                Debug.Log($"[Character] Stacked status '{effectData.statusEffectToApply.statusNameKey}' on {GetName()}. Stacks: {existingEffect.CurrentStacks}, Duration: {existingEffect.RemainingDuration}");
            }
            else // Overwrite/Refresh
            {
                existingEffect.RemainingDuration = effectData.duration;
                existingEffect.Potency = calculatedPotency;
                existingEffect.Caster = caster; // Update caster if it can change
                Debug.Log($"[Character] Refreshed status '{effectData.statusEffectToApply.statusNameKey}' on {GetName()}. Duration: {existingEffect.RemainingDuration}");
            }
        }
        else
        {
            ActiveStatusEffect newStatus = new ActiveStatusEffect(
                effectData.statusEffectToApply,
                caster,
                effectData.elementType,
                calculatedPotency,
                effectData.duration
            );
            activeStatusEffects.Add(newStatus);
            Debug.Log($"[Character] Applied status '{newStatus.Definition.statusNameKey}' to {GetName()} from {caster.GetName()}. Duration: {newStatus.RemainingDuration}, Potency: {newStatus.Potency}, Element: {newStatus.EffectElementType}");
            // TODO: Trigger OnApply visual/audio effects from newStatus.Definition
        }
    }

    public void TickStatusEffects(SkillEffectProcessor processor)
    {
        if (!IsAlive) return;

        // Tick temporary stat modifiers first (like Defend stance)
        TickTemporaryModifiers();

        // Iterate backwards for safe removal
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect status = activeStatusEffects[i];
            
            // Apply tick effect
            processor.ProcessStatusEffectTick(this, status); // 'this' is the target

            // Decrement duration
            status.RemainingDuration--;

            if (status.RemainingDuration <= 0)
            {
                Debug.Log($"[Character] Status '{status.Definition.statusNameKey}' expired on {GetName()}.");
                // TODO: Trigger OnExpire visual/audio effects
                activeStatusEffects.RemoveAt(i);
            }
        }
    }


    public virtual void Die()
    {
        bool wasAlreadyDeadOrDying = _currentHealth <= 0 || stateMachine.CurrentState is DeathState;
        _currentHealth = 0; 
        if (wasAlreadyDeadOrDying && stateMachine.CurrentState is DeathState) return;
        Debug.Log($"<color=black>[CHARACTER] {GetName()} has died. Changing to DeathState.</color>");
        stateMachine.ChangeState(new DeathState(this)); 
    }
}
