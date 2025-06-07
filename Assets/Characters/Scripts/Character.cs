using UnityEngine;
using System;
using DankestDungeon.Skills; // Assuming your enums are here
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;


public class Character : MonoBehaviour
{
    [SerializeField] private CharacterStats stats;
    public CharacterStats Stats => stats; // Make stats publicly accessible
    

    [SerializeField] public Animator characterAnimator;    
    [SerializeField] private bool debugStateTransitions = false;
    
    internal int _currentHealth;
    public int CurrentHealth => _currentHealth;
    public bool IsAlive => _currentHealth > 0;
    public int FormationPosition { get; set; }
    
    private StateMachine<CharacterState> stateMachine;

    private AnimationType currentAnimationType = AnimationType.None;
    private Action _onAnimationCompleteCallback;

    public string GetName()
    {
        if (stats != null && !string.IsNullOrEmpty(stats.characterName))
        {
            return stats.characterName;
        }
        // Fallback to GameObject name if stats or characterName is not set
        return gameObject.name;
    }

    public IState GetCurrentState()
    {
        return stateMachine?.CurrentState;
    }
    
    // === SERIALIZABLE SKILL SYSTEM ===
    [System.Serializable]
    public class SkillEntry
    {
        public SkillDefinitionSO skill;
        [Range(1, 5)]
        public int rank = 1;
        
        public SkillEntry() { }
        public SkillEntry(SkillDefinitionSO skill, int rank)
        {
            this.skill = skill;
            this.rank = rank;
        }
    }

    [BoxGroup("Learned Skills", CenterLabel = true)]
    [PropertyOrder(100)]
    [ListDrawerSettings(
        DraggableItems = false,
        HideAddButton = false,
        HideRemoveButton = false,
        ShowItemCount = true,
        CustomAddFunction = "AddSkillEntryFromUI"
    )]
    [SerializeField]
    private List<SkillEntry> serializedSkills = new List<SkillEntry>();

    // Runtime dictionary for fast lookups - populated from serializedSkills
    private Dictionary<SkillDefinitionSO, int> learnedSkills = new Dictionary<SkillDefinitionSO, int>();

    private void AddSkillEntryFromUI()
    {
        serializedSkills.Add(new SkillEntry());
    }

    [BoxGroup("Learned Skills")]
    [PropertyOrder(101)]
    [InfoBox("Skills are stored in the list above. Use the buttons below for additional management options.", InfoMessageType.Info)]
    [Button("Sync Skills to Dictionary", ButtonSizes.Medium), GUIColor(0.6f, 0.8f, 1f)]
    private void SyncSkillsFromSerialized()
    {
        learnedSkills.Clear();
        
        // Remove null/invalid entries
        serializedSkills.RemoveAll(entry => entry.skill == null);
        
        // Validate and sync to dictionary
        for (int i = 0; i < serializedSkills.Count; i++)
        {
            var entry = serializedSkills[i];
            
            // Validate rank
            int maxRank = entry.skill.ranks?.Count ?? 0;
            if (maxRank == 0)
            {
                Debug.LogWarning($"Skill {entry.skill.skillNameKey} has no ranks defined. Removing.");
                serializedSkills.RemoveAt(i);
                i--; // Adjust index after removal
                continue;
            }
            
            if (entry.rank < 1 || entry.rank > maxRank)
            {
                Debug.LogWarning($"Invalid rank {entry.rank} for skill {entry.skill.skillNameKey}. Clamping to valid range [1-{maxRank}].");
                entry.rank = Mathf.Clamp(entry.rank, 1, maxRank);
            }
            
            // Add to dictionary (handle duplicates by keeping highest rank)
            if (learnedSkills.ContainsKey(entry.skill))
            {
                if (entry.rank > learnedSkills[entry.skill])
                {
                    learnedSkills[entry.skill] = entry.rank;
                    Debug.Log($"Updated {entry.skill.skillNameKey} to rank {entry.rank}");
                }
            }
            else
            {
                learnedSkills[entry.skill] = entry.rank;
            }
        }
        
        // Remove duplicates from serialized list
        var uniqueSkills = new Dictionary<SkillDefinitionSO, SkillEntry>();
        foreach (var entry in serializedSkills)
        {
            if (uniqueSkills.ContainsKey(entry.skill))
            {
                // Keep the higher rank entry
                if (entry.rank > uniqueSkills[entry.skill].rank)
                {
                    uniqueSkills[entry.skill] = entry;
                }
            }
            else
            {
                uniqueSkills[entry.skill] = entry;
            }
        }
        
        serializedSkills = uniqueSkills.Values.ToList();
        
        Debug.Log($"Synced {learnedSkills.Count} skills to dictionary for {GetName()}");
    }

    [BoxGroup("Learned Skills")]
    [PropertyOrder(102)]
    [Button("Clear All Skills", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
    private void ClearAllSkills()
    {
        if (serializedSkills.Count == 0)
        {
            Debug.Log($"{GetName()} has no skills to clear.");
            return;
        }
        
        int skillCount = serializedSkills.Count;
        serializedSkills.Clear();
        learnedSkills.Clear();
        Debug.Log($"Cleared {skillCount} skills from {GetName()}");
    }

    [BoxGroup("Learned Skills")]
    [PropertyOrder(103)]
    [Title("Quick Add Skill", titleAlignment: TitleAlignments.Centered)]
    [SerializeField, HideLabel]
    [InfoBox("Select a skill to add and specify the rank.", InfoMessageType.None)]
    private SkillDefinitionSO skillToAdd;

    [BoxGroup("Learned Skills")]
    [PropertyOrder(104)]
    [SerializeField, Range(1, 5), SuffixLabel("Rank")]
    [ShowIf("@skillToAdd != null")]
    private int rankToAdd = 1;

    [BoxGroup("Learned Skills")]
    [PropertyOrder(105)]
    [Button("Add/Upgrade Skill", ButtonSizes.Large), GUIColor(0.6f, 1f, 0.6f)]
    [EnableIf("@skillToAdd != null")]
    private void AddSkillFromEditor()
    {
        if (skillToAdd == null)
        {
            Debug.LogWarning("No skill selected to add.");
            return;
        }
        
        LearnSkill(skillToAdd, rankToAdd);
        
        // Clear the selection after adding
        skillToAdd = null;
        rankToAdd = 1;
    }

    [BoxGroup("Learned Skills")]
    [PropertyOrder(106)]
    [Title("Remove Skill", titleAlignment: TitleAlignments.Centered)]
    [ValueDropdown("GetLearnedSkillsList")]
    [SerializeField, HideLabel]
    [InfoBox("Select a skill to remove from this character.", InfoMessageType.None)]
    private SkillDefinitionSO skillToRemove;

    [BoxGroup("Learned Skills")]
    [PropertyOrder(107)]
    [Button("Remove Skill", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.6f)]
    [EnableIf("@skillToRemove != null")]
    private void RemoveSkillFromEditor()
    {
        if (skillToRemove == null)
        {
            Debug.LogWarning("No skill selected to remove.");
            return;
        }
        
        ForgetSkill(skillToRemove);
        skillToRemove = null;
    }

    [BoxGroup("Learned Skills")]
    [PropertyOrder(108)]
    [Title("Quick Actions", titleAlignment: TitleAlignments.Centered)]
    [HorizontalGroup("Learned Skills/QuickActions")]
    [Button("Load Default Skills"), GUIColor(0.8f, 0.8f, 1f)]
    private void LoadDefaultSkills()
    {
        // This method can be customized per character type or use a ScriptableObject reference
        // For now, it's a placeholder that you can customize
        Debug.Log($"LoadDefaultSkills called for {GetName()}. Implement character-specific default skills here.");
        
        // Example implementation:
        // if (characterClass == CharacterClass.Warrior)
        // {
        //     LoadWarriorDefaultSkills();
        // }
        // else if (characterClass == CharacterClass.Mage)
        // {
        //     LoadMageDefaultSkills();
        // }
    }

    [BoxGroup("Learned Skills")]
    [HorizontalGroup("Learned Skills/QuickActions")]
    [Button("Export Skills"), GUIColor(0.8f, 1f, 0.8f)]
    private void ExportSkills()
    {
        // Export current skills to console/log for easy copying
        if (learnedSkills.Count == 0)
        {
            Debug.Log($"{GetName()} has no skills to export.");
            return;
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Skills for {GetName()} ===");
        foreach (var kvp in learnedSkills)
        {
            sb.AppendLine($"- {kvp.Key.skillNameKey} (Rank {kvp.Value})");
        }
        sb.AppendLine("========================");
        Debug.Log(sb.ToString());
    }

    // Helper method for ValueDropdown
    private IEnumerable<SkillDefinitionSO> GetLearnedSkillsList()
    {
        return learnedSkills.Keys;
    }

    public IReadOnlyDictionary<SkillDefinitionSO, int> LearnedSkills => learnedSkills;

    public void LearnSkill(SkillDefinitionSO skill, int rank = 1)
    {
        if (skill == null)
        {
            Debug.LogError($"Attempted to learn a null skill on {GetName()}");
            return;
        }
        
        int maxRank = skill.ranks?.Count ?? 0;
        if (maxRank == 0)
        {
            Debug.LogError($"Skill {skill.skillNameKey} has no ranks defined. Cannot add.");
            return;
        }
        
        if (rank < 1 || rank > maxRank)
        {
            Debug.LogWarning($"Attempted to learn skill {skill.skillNameKey} at invalid rank {rank}. Max ranks: {maxRank}. Clamping to valid range.");
            rank = Mathf.Clamp(rank, 1, maxRank);
        }

        bool wasUpgrade = false;
        
        // Update runtime dictionary
        if (learnedSkills.ContainsKey(skill))
        {
            if (rank > learnedSkills[skill])
            {
                learnedSkills[skill] = rank;
                wasUpgrade = true;
                Debug.Log($"{GetName()} upgraded skill {skill.skillNameKey} to rank {rank}");
            }
            else
            {
                Debug.Log($"{GetName()} already knows {skill.skillNameKey} at rank {learnedSkills[skill]} or higher (attempted {rank}).");
                return; // No change needed
            }
        }
        else
        {
            learnedSkills.Add(skill, rank);
            Debug.Log($"{GetName()} learned new skill {skill.skillNameKey} at rank {rank}");
        }
        
        // Update serialized list
        var existingEntry = serializedSkills.FirstOrDefault(e => e.skill == skill);
        if (existingEntry != null)
        {
            existingEntry.rank = rank;
        }
        else
        {
            serializedSkills.Add(new SkillEntry(skill, rank));
        }
    }

    public void ForgetSkill(SkillDefinitionSO skill)
    {
        if (skill == null) return;
        
        if (learnedSkills.Remove(skill))
        {
            Debug.Log($"{GetName()} forgot skill {skill.skillNameKey}");
            
            // Remove from serialized list
            serializedSkills.RemoveAll(entry => entry.skill == skill);
        }
    }

    public bool KnowsSkill(SkillDefinitionSO skill)
    {
        return skill != null && learnedSkills.ContainsKey(skill);
    }

    public int GetSkillRank(SkillDefinitionSO skill)
    {
        if (KnowsSkill(skill))
        {
            return learnedSkills[skill];
        }
        return 0; // 0 if not known
    }

    public SkillRankData GetSkillRankData(SkillDefinitionSO skill)
    {
        if (KnowsSkill(skill))
        {
            int currentRank = learnedSkills[skill];
            // SkillDefinitionSO.ranks is 0-indexed list, but ranks are 1-indexed
            if (currentRank > 0 && currentRank <= skill.ranks.Count)
            {
                return skill.ranks[currentRank - 1];
            }
            else
            {
                Debug.LogError($"Skill {skill.skillNameKey} for character {GetName()} has current rank {currentRank}, but rank data is out of bounds (max ranks: {skill.ranks.Count}). Returning null.");
            }
        }
        Debug.LogWarning($"Character {GetName()} does not know skill {skill?.skillNameKey} or skill is null. Cannot get rank data.");
        return null;
    }

    // Example of how you might get a list of usable skills
    public List<SkillDefinitionSO> GetAvailableSkills()
    {
        return learnedSkills.Keys.ToList();
    }

    // Add this to the top of your Character class with other fields
    [System.Serializable]
    public class TemporaryModifier
    {
        public StatType statType;
        public float value;
        public int duration; // Number of turns remaining
        public bool isBuff;
        public string sourceName; // What skill/effect applied this modifier
    }

    // Add this field to store active modifiers
    private List<TemporaryModifier> activeTemporaryModifiers = new List<TemporaryModifier>();

    protected virtual void Awake()
    {
        // Initialize animator if not set
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
        }
        
        // Initialize state machine
        stateMachine = new StateMachine<CharacterState>();
        
        if (debugStateTransitions)
        {
            stateMachine.OnStateChanged += (oldState, newState) =>
                Debug.Log($"{GetName()}: {oldState?.GetType().Name} -> {newState?.GetType().Name}");
        }
        
        // Initialize character stats
        _currentHealth = stats.maxHealth;
        
        // IMPORTANT: Sync skills from serialized data to runtime dictionary
        SyncSkillsFromSerialized();
    }
    
    protected virtual void Start()
    {
        // Start in idle state
        stateMachine.Initialize(new IdleState(this));
    }
    
    protected virtual void Update()
    {
        stateMachine.Update(); // Update the current state

        // Check if the current state has completed and produced an event
        if (stateMachine.CheckStateCompletion(out object result) && result is CharacterEvent characterEvent)
        {
            HandleStateEvent(characterEvent);
        }
    }

    private void HandleStateEvent(CharacterEvent characterEvent)
    {
        Debug.Log($"<color=lightblue>[CHARACTER] {GetName()} handling CharacterEvent: {characterEvent}</color>");
        switch (characterEvent)
        {
            case CharacterEvent.AttackComplete:
            case CharacterEvent.HitComplete:
            case CharacterEvent.DefendComplete:
            case CharacterEvent.MagicComplete:
            case CharacterEvent.ItemComplete:
                // For most action completions, if the character is alive, transition to Idle.
                if (IsAlive)
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                // If not alive, the DeathState should have taken over or will soon.
                // No explicit transition here if already dead.
                break;
            case CharacterEvent.HitKilled:
                // This event implies the character died as a result of a hit.
                // The Die() method should have already been called, leading to DeathState.
                // No explicit transition here, but good for logging or other systems.
                Debug.Log($"[CHARACTER] {GetName()} was killed by a hit. DeathState should be active.");
                break;
            case CharacterEvent.DeathComplete:
                // The death animation/sequence has finished.
                // The character remains in a "dead" state conceptually.
                // No state change here, but other systems might react (e.g., BattleManager checking for team wipe).
                Debug.Log($"[CHARACTER] {GetName()} death sequence fully complete.");
                break;
            case CharacterEvent.None:
                // No specific action, or an event that doesn't require a state change here.
                break;
            default:
                Debug.LogWarning($"[CHARACTER] Unhandled CharacterEvent: {characterEvent} for {GetName()}");
                // Fallback to Idle if alive and in an unexpected situation.
                if (IsAlive && !(GetCurrentState() is IdleState))
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                break;
        }
    }
    
    // Public methods to change states
    // --- State Machine and Animation Control ---
    public void PlayAnimation(AnimationType type, Character targetForState = null)
    {
        if (characterAnimator == null)
        {
            Debug.LogWarning($"{GetName()} has no Animator component. Cannot play animation {type}.");
            // If an action relies on animation completion, we might need to immediately complete or skip.
            // For now, we assume the state change will handle logic if animation is absent.
        }

        currentAnimationType = type; // Track the intended animation
        Debug.Log($"[CHARACTER] {GetName()} attempting to play animation: {type} and transition state.");

        switch (type)
        {
            case AnimationType.Idle:
                stateMachine.ChangeState(new IdleState(this));
                break;
            case AnimationType.Attack:
                // AttackState needs a target. If targetForState is null, it might be an issue or a generic attack anim.
                stateMachine.ChangeState(new AttackState(this, targetForState));
                break;
            case AnimationType.Hit:
                // HitState doesn't usually need a target passed to its constructor, it's about self-reaction.
                stateMachine.ChangeState(new HitState(this));
                break;
            case AnimationType.Defend:
                stateMachine.ChangeState(new DefendState(this));
                break;
            case AnimationType.Cast:
                // MagicCastState needs a target.
                stateMachine.ChangeState(new MagicCastState(this, targetForState));
                break;
            case AnimationType.Item:
                // ItemState might need a target.
                stateMachine.ChangeState(new ItemState(this, targetForState));
                break;
            case AnimationType.Death:
                // DeathState is usually handled by Die() method directly.
                // But if an animation trigger is needed:
                stateMachine.ChangeState(new DeathState(this));
                break;
            case AnimationType.None:
                Debug.LogWarning($"{GetName()} was asked to play AnimationType.None. Ensuring IdleState.");
                if (!(GetCurrentState() is IdleState) && IsAlive) // Use IsAlive property
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                break;
            default:
                Debug.LogWarning($"Unhandled AnimationType '{type}' in PlayAnimation for state transition. Character: {GetName()}");
                if (characterAnimator != null) // Simplified animator check
                {
                    string triggerName = type.ToString();
                    // A common pattern is to have an Animator parameter with the same name as the enum value
                    // You might need a more sophisticated mapping if names differ.
                    try
                    {
                        characterAnimator.SetTrigger(triggerName);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to set trigger '{triggerName}' for {GetName()}: {e.Message}. Checking for generic 'Action' trigger.");
                        // Check if a generic "Action" trigger exists if specific one is missing
                        try { characterAnimator.SetTrigger("Action"); }
                        catch { Debug.LogWarning($"Animator for {GetName()} does not have a state/trigger for {triggerName} or a generic 'Action' trigger."); }
                    }
                }
                
                if (IsAlive && GetCurrentState() == null) // Use IsAlive property
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                break;
        }
    }

    // --- Action Methods ---
    // These methods now primarily delegate to PlayAnimation for state changes and animation triggering.

    public void Attack(Character target)
    {
        if (!IsAlive) return; // Use IsAlive property
        Debug.Log($"<color=orange>[CHARACTER] {GetName()} initiating Attack on {target?.GetName()} via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Attack, target);
    }

    public void TakeHit()
    {
        if (!IsAlive) return; // Use IsAlive property
        Debug.Log($"<color=red>[CHARACTER] {GetName()} initiating TakeHit via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Hit);
    }

    public void Defend()
    {
        if (!IsAlive) return; // Use IsAlive property
        Debug.Log($"<color=blue>[CHARACTER] {GetName()} initiating Defend via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Defend);
    }

    public void CastMagic(Character target) 
    {
        if (!IsAlive) return; // Use IsAlive property
        Debug.Log($"<color=purple>[CHARACTER] {GetName()} initiating magic cast on {target?.GetName() ?? "self/area"} via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Cast, target);
    }

    public void UseItem(Character target) 
    {
        if (!IsAlive) return; // Use IsAlive property
        Debug.Log($"<color=green>[CHARACTER] {GetName()} initiating item use on {target?.GetName() ?? "self/area"} via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Item, target);
    }

    public virtual void Die() // Changed to virtual
    {
        if (!IsAlive) return; // Use IsAlive property. Check before setting health to 0.
                              // This ensures Die() logic runs only once.
        
        _currentHealth = 0; // This will make IsAlive return false
        Debug.Log($"<color=black>[CHARACTER] {GetName()} has died. Changing to DeathState.</color>");
        
        stateMachine.ChangeState(new DeathState(this));
        
        // OnCharacterDied?.Invoke(this); // Event for other systems
    }

    // Renamed from ReceiveDamage to TakeDamage for consistency
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return; // Use IsAlive property
        int oldHealth = _currentHealth;
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(0, _currentHealth);

        Debug.Log($"[CHARACTER] {GetName()} received {amount} damage. Health: {oldHealth} -> {_currentHealth}");
        // OnHealthChanged?.Invoke(_currentHealth, stats.maxHealth);

        if (_currentHealth <= 0 && oldHealth > 0) 
        {
            Die();
        }
    }
    
    public void HealDamage(int amount)
    {
        if (!IsAlive) // Use IsAlive property (though healing dead might be a revive mechanic later)
        {
            Debug.LogWarning($"Attempted to heal {GetName()} but they are not alive.");
            return;
        }
        if (amount <= 0) return;

        int oldHealth = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, stats.maxHealth); // Cap at max health

        Debug.Log($"[CHARACTER] {GetName()} healed {amount} damage. Health: {oldHealth} -> {_currentHealth}");
        // OnHealthChanged?.Invoke(_currentHealth, stats.maxHealth);
    }

    public void ApplyDefenseStance()
    {
        if (!IsAlive) return; // Use IsAlive property
        // Logic to apply defense buff/status
        Debug.Log($"[CHARACTER] {GetName()} is now defending.");
        // Example: AddTemporaryModifier(StatType.Defense, Stats.defense * 0.5f, 1, true, "Defend Action");
    }
    
    // Ensure stat getters are implemented
    public float GetAttackPower() { return stats != null ? stats.attackPower : 0; /* + temp modifiers */ }
    public float GetDefense() { return stats != null ? stats.defense : 0; /* + temp modifiers */ }
    public float GetMagicPower() { return stats != null ? stats.magicPower : 0; /* + temp modifiers */ }
    public float GetMagicResistance() { return stats != null ? stats.magicResistance : 0; /* + temp modifiers */ }
    public float GetCritChance() { return stats != null ? stats.criticalChance : 0; /* + temp modifiers */ }
    public float GetSpeed() { return stats != null ? stats.speed : 0; /* + temp modifiers */ }

    public void AddTemporaryModifier(StatType statToModify, float modValue, int duration, bool isBuff, string sourceName = "Unknown")
    {
        // Implementation for adding temporary modifiers
        // activeTemporaryModifiers.Add(new TemporaryModifier { ... });
        Debug.Log($"[CHARACTER] {GetName()} received stat mod: {statToModify} by {modValue} for {duration} turns. IsBuff: {isBuff}. Source: {sourceName}");
    }

    private void UpdateTemporaryModifiers()
    {
        // Logic to decrement duration and remove expired modifiers
    }

    // Method to be called by CharacterVisuals when an animation event fires
    public void OnAnimationComplete()
    {
        Debug.Log($"<color=cyan>[CHARACTER] {GetName()} received OnAnimationComplete signal.</color>");
        Action callback = _onAnimationCompleteCallback;
        _onAnimationCompleteCallback = null; // Clear the callback after retrieving it to prevent multiple calls

        if (callback != null)
        {
            Debug.Log($"<color=cyan>[CHARACTER] {GetName()} invoking registered animation complete callback.</color>");
            callback.Invoke();
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[CHARACTER] {GetName()} OnAnimationComplete called, but no callback was registered. Current state: {stateMachine?.CurrentState?.GetType().Name}</color>");
            // If no callback, and we are in a state that should transition on animation,
            // we might need a fallback to IdleState, but states should ideally handle their own transitions.
            // For now, this warning is important.
        }
    }

    // Method for states to register a callback for animation completion
    public void RegisterAnimationCallback(Action callback)
    {
        // Clear any existing callback before registering a new one to avoid conflicts
        if (_onAnimationCompleteCallback != null)
        {
            Debug.LogWarning($"<color=yellow>[CHARACTER] {GetName()} overwriting an existing animation complete callback. Old callback was for: (State might have changed). New callback registered.</color>");
        }
        _onAnimationCompleteCallback = callback;
        Debug.Log($"<color=cyan>[CHARACTER] {GetName()} registered an animation complete callback. Current state: {stateMachine?.CurrentState?.GetType().Name}</color>");
    }

    // Optional: Method for states to clear their callback if they exit prematurely
    public void ClearAnimationCallback()
    {
        _onAnimationCompleteCallback = null;
    }
}