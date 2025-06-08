using UnityEngine;
using System;
using DankestDungeon.Skills; // Assuming your enums are here
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

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
    public CharacterBuffs Buffs => characterBuffs; // Public accessor


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
        characterBuffs = new CharacterBuffs(GetName()); // Pass character name for logging
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

    public void PlayAnimation(AnimationType type, Character targetForState = null)
    {
        if (characterAnimator == null && type != AnimationType.None && type != AnimationType.Idle)
        {
            Debug.LogWarning($"{GetName()} has no Animator component. Cannot play animation {type}.");
        }
        currentAnimationType = type;
        switch (type)
        {
            case AnimationType.Idle: stateMachine.ChangeState(new IdleState(this)); break;
            case AnimationType.Attack: stateMachine.ChangeState(new AttackState(this, targetForState)); break;
            case AnimationType.Hit: stateMachine.ChangeState(new HitState(this)); break;
            case AnimationType.Defend: stateMachine.ChangeState(new DefendState(this)); break;
            case AnimationType.Cast: stateMachine.ChangeState(new MagicCastState(this, targetForState)); break;
            case AnimationType.Item: stateMachine.ChangeState(new ItemState(this, targetForState)); break;
            case AnimationType.Death: stateMachine.ChangeState(new DeathState(this)); break;
            case AnimationType.None:
                if (!(GetCurrentState() is IdleState) && IsAlive) stateMachine.ChangeState(new IdleState(this));
                break;
            default:
                Debug.LogWarning($"Unhandled AnimationType '{type}' in PlayAnimation for {GetName()}.");
                if (characterAnimator != null) { try { characterAnimator.SetTrigger(type.ToString()); } catch (Exception e) { Debug.LogWarning($"Failed to set trigger '{type.ToString()}' for {GetName()}: {e.Message}."); } }
                if (IsAlive && GetCurrentState() == null) stateMachine.ChangeState(new IdleState(this));
                break;
        }
    }

    public void Attack(Character target) { if (IsAlive) PlayAnimation(AnimationType.Attack, target); }
    public void TakeHit() { if (IsAlive) PlayAnimation(AnimationType.Hit); }
    public void Defend() { if (IsAlive) PlayAnimation(AnimationType.Defend); }
    public void CastMagic(Character target) { if (IsAlive) PlayAnimation(AnimationType.Cast, target); }
    public void UseItem(Character target) { if (IsAlive) PlayAnimation(AnimationType.Item, target); }

    public void RegisterAnimationCallback(Action callback) => _onAnimationCompleteCallback = callback;
    public void ClearAnimationCallback() => _onAnimationCompleteCallback = null;
    public void OnAnimationComplete()
    {
        Action callback = _onAnimationCompleteCallback;
        _onAnimationCompleteCallback = null;
        callback?.Invoke();
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

    public virtual void Die()
    {
        bool wasAlreadyDeadOrDying = _currentHealth <= 0 || stateMachine.CurrentState is DeathState;
        _currentHealth = 0; 
        if (wasAlreadyDeadOrDying && stateMachine.CurrentState is DeathState) return;
        Debug.Log($"<color=black>[CHARACTER] {GetName()} has died. Changing to DeathState.</color>");
        stateMachine.ChangeState(new DeathState(this)); 
    }
}
