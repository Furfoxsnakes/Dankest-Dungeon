using UnityEngine;
using System;
using DankestDungeon.Skills; // Assuming your enums are here
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using DankestDungeon.Characters; // For ActiveStatusEffect
using DankestDungeon.StatusEffects; // For StatusEffectSO
// Add this if not already present for StatType, or ensure StatType is globally accessible
// using DankestDungeon.Skills; // Assuming StatType is here

public class Character : MonoBehaviour
{
    [SerializeField] private CharacterStats stats;
    public CharacterStats Stats => stats;

    [SerializeField] public Animator characterAnimator;
    [SerializeField] private bool debugStateTransitions = false;
    [SerializeField] private CharacterVisuals characterVisuals; // Add reference to CharacterVisuals

    internal int _currentHealth;
    public int CurrentHealth => _currentHealth;
    internal int _currentMana; // Add this line
    public int CurrentMana => _currentMana; // Add this line
    public bool IsAlive => _currentHealth > 0;
    public int FormationPosition { get; set; }

    private StateMachine<CharacterState> stateMachine;
    private AnimationType currentAnimationType = AnimationType.None;
    private Action _onAnimationCompleteCallback;

    // === MODULES ===
    private CharacterBuffs characterBuffs;
    public CharacterBuffs Buffs => characterBuffs;

    [Header("Component References")] // You can use a header for clarity
    [SerializeField] private CharacterEquipment characterEquipment; // Changed to SerializeField
    public CharacterEquipment Equipment => characterEquipment;

    private List<ActiveStatusEffect> activeStatusEffects = new List<ActiveStatusEffect>();
    public IReadOnlyList<ActiveStatusEffect> ActiveStatusEffects => activeStatusEffects.AsReadOnly();


    public string GetName()
    {
        return stats != null && !string.IsNullOrEmpty(stats.characterName) ? stats.characterName : gameObject.name;
    }

    public IState GetCurrentState() => stateMachine?.CurrentState;

    // === ODIN INSPECTOR QUICK ACTIONS (moved some to CharacterSkills) ===
    // [BoxGroup("Character Quick Actions", CenterLabel = true)] // REMOVED SKILL BUTTONS
    // [PropertyOrder(100)]
    // [Button("Load Default Skills (Character-Specific)"), GUIColor(0.8f, 0.8f, 1f)]
    // private void LoadDefaultSkills() // REMOVED
    // {
    //     Debug.Log($"LoadDefaultSkills called for {GetName()}. Implement character-specific default skills here. Skills can be added via Character.Skills.LearnSkill().");
    // }

    // [BoxGroup("Character Quick Actions")] // REMOVED SKILL BUTTONS
    // [PropertyOrder(101)]
    // [Button("Export Skills (Console)"), GUIColor(0.8f, 1f, 0.8f)]
    // private void ExportSkillsToConsole() // REMOVED
    // {
    //     System.Text.StringBuilder sb = new System.Text.StringBuilder();
    //     Skills.ExportSkillsToString(sb); 
    //     Debug.Log(sb.ToString());
    // }


    // === PUBLIC SKILL ACCESSORS (delegating to CharacterSkills) === // REMOVED ALL
    // public IReadOnlyDictionary<SkillDefinitionSO, int> LearnedSkills => Skills.LearnedSkills;
    // public void LearnSkill(SkillDefinitionSO skill, int rank = 1) => Skills.LearnSkill(skill, rank);
    // public void ForgetSkill(SkillDefinitionSO skill) => Skills.ForgetSkill(skill);
    // public bool KnowsSkill(SkillDefinitionSO skill) => Skills.KnowsSkill(skill);
    // public int GetSkillRank(SkillDefinitionSO skill) => Skills.GetSkillRank(skill);
    // public SkillRankData GetSkillRankData(SkillDefinitionSO skill) => Skills.GetSkillRankData(skill);
    // public List<SkillDefinitionSO> GetAvailableSkills() => Skills.GetAvailableSkills();


    protected virtual void Awake()
    {
        if (characterAnimator == null) characterAnimator = GetComponent<Animator>();
        if (characterVisuals == null) characterVisuals = GetComponentInChildren<CharacterVisuals>();
        if (characterVisuals == null) Debug.LogError($"CharacterVisuals not found for {GetName()}!");
        
        stateMachine = new StateMachine<CharacterState>();
        if (debugStateTransitions)
        {
            stateMachine.OnStateChanged += (oldState, newState) =>
                Debug.Log($"{GetName()}: {oldState?.GetType().Name} -> {newState?.GetType().Name}");
        }

        if (stats == null) Debug.LogError($"CharacterStats not assigned for {GetName()}!");
        else
        {
            _currentHealth = stats.maxHealth;
            _currentMana = stats.maxMana;
        }

        // Initialize Modules
        characterBuffs = new CharacterBuffs(GetName());
        activeStatusEffects = new List<ActiveStatusEffect>();

        // Ensure CharacterEquipment is assigned in the Inspector
        if (characterEquipment == null)
        {
            Debug.LogError($"CharacterEquipment is not assigned in the Inspector for {GetName()}! Please assign it.");
            // Optionally, you could still try to find it or add it as a fallback,
            // but the primary intention is now manual assignment.
            // characterEquipment = GetComponentInChildren<CharacterEquipment>(true); // Fallback example
            // if (characterEquipment == null) characterEquipment = gameObject.AddComponent<CharacterEquipment>(); // Further fallback
        }
        
        // Optional: Subscribe to equipment changes if needed for immediate UI updates or other logic
        // if (characterEquipment != null)
        // {
        //     characterEquipment.OnEquipmentChanged += HandleEquipmentChanged;
        // }
    }

    // Optional: Handler for equipment changes
    // private void HandleEquipmentChanged()
    // {
    //     Debug.Log($"{GetName()}'s equipment changed. Recalculating relevant stats or notifying UI.");
    //     // Example: Update health if MaxHealth changed
    //     // int newMaxHealth = GetMaxHealth();
    //     // _currentHealth = Mathf.Min(_currentHealth, newMaxHealth);
    // }

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
        Action externalCb = _externalAnimationCompleteCallback;
        if (externalCb != null)
        {
            _externalAnimationCompleteCallback = null; // Clear it immediately
            Debug.Log($"[{gameObject.name}] Invoking EXTERNAL animation complete callback. (ActionSequenceHandler should handle this)");
            externalCb.Invoke();
            // External callback (likely from ActionSequenceHandler) takes precedence.
            // Internal callback is not invoked to prevent double-processing.
        }
        else // No external callback, consider internal.
        {
            Action internalCb = _internalAnimationCompleteCallback;
            if (internalCb != null)
            {
                // If the character is currently in IdleState, an animation completing
                // is less likely to be the specific animation that an action state (e.g., AttackState)
                // registered an internal callback for. This heuristic aims to prevent an Idle
                // animation from prematurely consuming a callback intended for a subsequent action.
                if (stateMachine.CurrentState is IdleState)
                {
                    Debug.Log($"[{gameObject.name}] OnAnimationComplete called while in IdleState. An internal callback was pending but will NOT be invoked/cleared at this time, to preserve it for a potential action state. Animation events from IdleState should generally not consume critical action callbacks.");
                    // We do not invoke or clear internalCb here, assuming it's for a more specific state.
                    // This might mean the warning below still appears if only an Idle animation completed.
                }
                else
                {
                    // If not in IdleState, assume the internal callback is relevant to the current active state.
                    _internalAnimationCompleteCallback = null; // Clear it before invoking
                    Debug.Log($"[{gameObject.name}] Invoking INTERNAL animation complete callback (e.g., for AttackState, MagicCastState).");
                    internalCb.Invoke();
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] OnAnimationComplete called. No external callback was registered. No internal callback was pending or it was not consumed (e.g., due to being in IdleState).");
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

    public void PlayAnimation(AnimationTriggerName trigger, Character targetForAnim = null)
    {
        Debug.Log($"[{gameObject.name}] PlayAnimation called with trigger: {trigger}");
        if (characterVisuals != null)
        {
            characterVisuals.PlayAnimation(trigger);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] CharacterVisuals is null. Cannot play animation with trigger: {trigger}");
        }
    }

    public void Attack(Character target)
    {
        Debug.Log($"[{gameObject.name}] Attack called on {target?.name ?? "null target"}");
        PlayAnimation(AnimationTriggerName.Attack, target);
    }

    public void TakeHit()
    {
        Debug.Log($"[{gameObject.name}] TakeHit called");
        PlayAnimation(AnimationTriggerName.Hit);
    }

    public void Defend()
    {
        if (!IsAlive || stateMachine.CurrentState is DeathState) return;
        Debug.Log($"[{gameObject.name}] Defend called");
        // Assuming you have a DefendState similar to AttackState or IdleState
        stateMachine.ChangeState(new DefendState(this)); // Ensure DefendState exists
    }

    public void CastMagic(Character target)
    {
        if (!IsAlive || stateMachine.CurrentState is DeathState) return;
        Debug.Log($"[{gameObject.name}] CastMagic called on {target?.name ?? "null target"}");
        // Assuming you have a MagicCastState
        stateMachine.ChangeState(new MagicCastState(this, target)); // Ensure MagicCastState exists and can take a target
    }

    public void UseItem(Character target)
    {
        if (!IsAlive || stateMachine.CurrentState is DeathState) return;
        Debug.Log($"[{gameObject.name}] UseItem called on {target?.name ?? "self/area"}");
        // Assuming you have an ItemState
        stateMachine.ChangeState(new ItemState(this, target)); // Ensure ItemState exists and can take a target
    }


    // --- Stat Getters (incorporating equipment and temporary modifiers) ---
    public int GetAttackPower()
    {
        float baseValue = Stats != null ? Stats.attackPower : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.AttackPower, baseValue) - baseValue; // Get only the buff's contribution
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.AttackPower) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

    public int GetDefense()
    {
        float baseValue = Stats != null ? Stats.defense : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.Defense, baseValue) - baseValue;
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.Defense) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

    public int GetMagicPower()
    {
        float baseValue = Stats != null ? Stats.magicPower : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.MagicPower, baseValue) - baseValue;
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.MagicPower) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

    public int GetMagicResistance()
    {
        float baseValue = Stats != null ? Stats.magicResistance : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.MagicResistance, baseValue) - baseValue;
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.MagicResistance) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

    public float GetCritChance() // Crit chance is often a percentage
    {
        float baseValue = Stats != null ? Stats.criticalChance / 100f : 0f; // Assuming criticalChance is stored as 5 for 5%. Added 'f' for clarity.
        // The 'buffValue' line below was part of the issue if GetModifiedStatValue already includes baseValue.
        // float buffValue = characterBuffs.GetModifiedStatValue(StatType.CritChance, baseValue); 

        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.CritChance) : 0f; // Assuming items add to the percentage (e.g., 0.05 for 5%)

        // For consistency with other stat calculation methods:
        // Calculate the buff's contribution separately from the base value.
        // This assumes GetModifiedStatValue returns the TOTAL value (base + buff),
        // so we subtract baseValue to get just the buff's effect.
        // If GetModifiedStatValue returns ONLY the buff amount, then you'd just add it.
        float buffContribution = characterBuffs.GetModifiedStatValue(StatType.CritChance, baseValue) - baseValue;
        
        return baseValue + buffContribution + equipBonus;
    }

    public int GetSpeed()
    {
        float baseValue = Stats != null ? Stats.speed : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.Speed, baseValue) - baseValue;
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.Speed) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

    public int GetMaxHealth()
    {
        float baseValue = Stats != null ? Stats.maxHealth : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.MaxHealth, baseValue) - baseValue;
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.MaxHealth) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

    public int GetMaxMana()
    {
        float baseValue = Stats != null ? Stats.maxMana : 0;
        float buffBonus = characterBuffs.GetModifiedStatValue(StatType.MaxMana, baseValue) - baseValue;
        float equipBonus = characterEquipment != null ? characterEquipment.GetBonusForStat(StatType.MaxMana) : 0;
        return Mathf.RoundToInt(baseValue + buffBonus + equipBonus);
    }

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

    public bool HasEnoughMana(int manaCost)
    {
        return _currentMana >= manaCost;
    }

    public void SpendMana(int manaCost)
    {
        if (manaCost <= 0) return;
        if (HasEnoughMana(manaCost))
        {
            int oldMana = _currentMana;
            _currentMana -= manaCost;
            Debug.Log($"[CHARACTER] {GetName()} spent {manaCost} mana. Mana: {oldMana} -> {_currentMana}");
        }
        else
        {
            Debug.LogWarning($"[CHARACTER] {GetName()} attempted to spend {manaCost} mana, but only has {_currentMana}. Mana not spent.");
        }
    }

    public void RestoreMana(int amount)
    {
        if (amount <= 0) return;
        int oldMana = _currentMana;
        _currentMana = Mathf.Min(_currentMana + amount, GetMaxMana());
        Debug.Log($"[CHARACTER] {GetName()} restored {amount} mana. Mana: {oldMana} -> {_currentMana}");
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
        // ...
        PlayAnimation(AnimationTriggerName.Death);
        // ...
    }
}
