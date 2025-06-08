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
            if (entry.skill == null) // Should be caught by RemoveAll, but as an extra check
            {
                Debug.LogWarning($"Found a null skill entry at index {i} during sync. Removing.");
                serializedSkills.RemoveAt(i);
                i--;
                continue;
            }


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
                    // Debug.Log($"Updated {entry.skill.skillNameKey} to rank {entry.rank}");
                }
            }
            else
            {
                learnedSkills[entry.skill] = entry.rank;
            }
        }

        // Remove duplicates from serialized list, keeping the one with the highest rank (which should match the dictionary)
        var uniqueSkills = new Dictionary<SkillDefinitionSO, SkillEntry>();
        foreach (var entry in serializedSkills)
        {
            if (entry.skill == null) continue; // Skip null skills

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
        // Ensure serialized list matches the dictionary's ranks
        serializedSkills = uniqueSkills.Values.ToList();
        foreach(var s_entry in serializedSkills)
        {
            if(learnedSkills.TryGetValue(s_entry.skill, out int dictRank))
            {
                s_entry.rank = dictRank;
            }
        }


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
    [ValueDropdown("GetLearnedSkillsListForEditor")] // Renamed for clarity
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
        Debug.Log($"LoadDefaultSkills called for {GetName()}. Implement character-specific default skills here.");
    }

    [BoxGroup("Learned Skills")]
    [HorizontalGroup("Learned Skills/QuickActions")]
    [Button("Export Skills"), GUIColor(0.8f, 1f, 0.8f)]
    private void ExportSkills()
    {
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
    private IEnumerable<SkillDefinitionSO> GetLearnedSkillsListForEditor() // Renamed
    {
        return learnedSkills.Keys.ToList(); // Ensure it's a list for Odin
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

        bool wasUpgrade = false; // Used for logging

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
            if (currentRank > 0 && skill.ranks != null && currentRank <= skill.ranks.Count)
            {
                return skill.ranks[currentRank - 1];
            }
            else
            {
                Debug.LogError($"Skill {skill.skillNameKey} for character {GetName()} has current rank {currentRank}, but rank data is out of bounds (max ranks: {skill.ranks?.Count ?? 0}). Returning null.");
            }
        }
        // Debug.LogWarning($"Character {GetName()} does not know skill {skill?.skillNameKey} or skill is null. Cannot get rank data.");
        return null;
    }

    public List<SkillDefinitionSO> GetAvailableSkills()
    {
        return learnedSkills.Keys.ToList();
    }

    [System.Serializable]
    public class TemporaryModifier
    {
        public StatType statType;
        public float value; // Can be flat or percentage based on how you use it
        public int duration; // Number of turns remaining
        public bool isBuff; // True for buff, false for debuff
        public string sourceName; // What skill/effect applied this modifier

        // Optional: Add a field for how the value is applied (e.g., flat, percent_add, percent_mult)
        // public ModifierApplicationType applicationType;
    }
    // public enum ModifierApplicationType { Flat, PercentAdd, PercentMult }


    private List<TemporaryModifier> activeTemporaryModifiers = new List<TemporaryModifier>();

    protected virtual void Awake()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
        }
        stateMachine = new StateMachine<CharacterState>();
        if (debugStateTransitions)
        {
            stateMachine.OnStateChanged += (oldState, newState) =>
                Debug.Log($"{GetName()}: {oldState?.GetType().Name} -> {newState?.GetType().Name}");
        }
        if (stats == null)
        {
            Debug.LogError($"CharacterStats not assigned for {GetName()}!");
            // Optionally assign a default or disable the character
        }
        else
        {
            _currentHealth = stats.maxHealth;
        }
        SyncSkillsFromSerialized();
    }

    protected virtual void Start()
    {
        stateMachine.Initialize(new IdleState(this));
    }

    protected virtual void Update()
    {
        stateMachine.Update(); // Calls CurrentState.UpdateLogic()

        // The 'result' from CheckStateCompletion is already an object.
        // We cast it to CharacterEvent if we are sure that's what Character states will produce.
        if (stateMachine.CheckStateCompletion(out object result) && result is CharacterEvent characterEvent)
        {
            HandleStateEvent(characterEvent);

            // After handling the event, reset the completion status of the current state.
            // CurrentState is guaranteed to be IState, which now has ResetCompletionStatus.
            stateMachine.CurrentState?.ResetCompletionStatus();
        }
    }

    private void HandleStateEvent(CharacterEvent characterEvent)
    {
        Debug.Log($"<color=lightblue>[CHARACTER] {GetName()} handling CharacterEvent: {characterEvent}. IsAlive: {IsAlive}</color>");
        switch (characterEvent)
        {
            case CharacterEvent.AttackComplete:
            case CharacterEvent.HitComplete:
            case CharacterEvent.DefendComplete:
            case CharacterEvent.MagicComplete:
            case CharacterEvent.ItemComplete:
                if (IsAlive)
                {
                    if (!(stateMachine.CurrentState is DeathState)) 
                    {
                        stateMachine.ChangeState(new IdleState(this));
                    }
                }
                break;
            case CharacterEvent.HitKilled:
                Debug.Log($"[CHARACTER] {GetName()} was killed by a hit. DeathState should be active or imminently active.");
                break;
            case CharacterEvent.DeathComplete:
                Debug.Log($"[CHARACTER] {GetName()} death sequence fully complete. Remains in DeathState.");
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

    // --- State Machine and Animation Control ---
    public void PlayAnimation(AnimationType type, Character targetForState = null)
    {
        if (characterAnimator == null && type != AnimationType.None && type != AnimationType.Idle) // Allow None/Idle without animator
        {
            Debug.LogWarning($"{GetName()} has no Animator component. Cannot play animation {type}.");
        }

        currentAnimationType = type;
        // Debug.Log($"[CHARACTER] {GetName()} attempting to play animation: {type} and transition state.");

        switch (type)
        {
            case AnimationType.Idle:
                stateMachine.ChangeState(new IdleState(this));
                break;
            case AnimationType.Attack:
                stateMachine.ChangeState(new AttackState(this, targetForState));
                break;
            case AnimationType.Hit:
                stateMachine.ChangeState(new HitState(this));
                break;
            case AnimationType.Defend:
                stateMachine.ChangeState(new DefendState(this));
                break;
            case AnimationType.Cast:
                stateMachine.ChangeState(new MagicCastState(this, targetForState));
                break;
            case AnimationType.Item:
                stateMachine.ChangeState(new ItemState(this, targetForState));
                break;
            case AnimationType.Death:
                stateMachine.ChangeState(new DeathState(this));
                break;
            case AnimationType.None:
                // Debug.LogWarning($"{GetName()} was asked to play AnimationType.None. Ensuring IdleState if alive.");
                if (!(GetCurrentState() is IdleState) && IsAlive)
                {
                    stateMachine.ChangeState(new IdleState(this));
                }
                break;
            default:
                Debug.LogWarning($"Unhandled AnimationType '{type}' in PlayAnimation for state transition. Character: {GetName()}");
                if (characterAnimator != null)
                {
                    string triggerName = type.ToString();
                    try { characterAnimator.SetTrigger(triggerName); }
                    catch (Exception e) { Debug.LogWarning($"Failed to set trigger '{triggerName}' for {GetName()}: {e.Message}."); }
                }
                if (IsAlive && GetCurrentState() == null)
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
        if (!IsAlive) return;
        // Debug.Log($"<color=orange>[CHARACTER] {GetName()} initiating Attack on {target?.GetName()} via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Attack, target);
    }

    public void TakeHit()
    {
        if (!IsAlive) return;
        // Debug.Log($"<color=red>[CHARACTER] {GetName()} initiating TakeHit via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Hit);
    }

    public void Defend()
    {
        if (!IsAlive) return;
        // Debug.Log($"<color=blue>[CHARACTER] {GetName()} initiating Defend via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Defend);
    }

    public void CastMagic(Character target)
    {
        if (!IsAlive) return;
        // Debug.Log($"<color=purple>[CHARACTER] {GetName()} initiating magic cast on {target?.GetName() ?? "self/area"} via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Cast, target);
    }

    public void UseItem(Character target)
    {
        if (!IsAlive) return;
        // Debug.Log($"<color=green>[CHARACTER] {GetName()} initiating item use on {target?.GetName() ?? "self/area"} via PlayAnimation.</color>");
        PlayAnimation(AnimationType.Item, target);
    }

    // --- Animation Callback System ---
    public void RegisterAnimationCallback(Action callback)
    {
        _onAnimationCompleteCallback = callback;
        // Debug.Log($"<color=cyan>[CHARACTER] {GetName()} registered an animation complete callback. Current state: {stateMachine?.CurrentState?.GetType().Name}</color>");
    }

    public void ClearAnimationCallback()
    {
        _onAnimationCompleteCallback = null;
        // Debug.Log($"<color=cyan>[CHARACTER] {GetName()} cleared animation complete callback.</color>");
    }

    // Called by CharacterVisuals (or an animation event directly on this GameObject)
    public void OnAnimationComplete()
    {
        // Debug.Log($"<color=cyan>[CHARACTER] {GetName()} received OnAnimationComplete signal.</color>");
        Action callback = _onAnimationCompleteCallback;
        _onAnimationCompleteCallback = null; // Clear immediately to prevent re-triggering if called multiple times rapidly
        
        if (callback != null)
        {
            // Debug.Log($"<color=cyan>[CHARACTER] {GetName()} invoking registered animation complete callback.</color>");
            callback.Invoke();
        }
        else
        {
            // Debug.Log($"<color=grey>[CHARACTER] {GetName()} OnAnimationComplete called, but no callback was registered or it was already cleared.</color>");
        }
    }

    // --- Stat Getters (incorporating temporary modifiers) ---
    private float GetModifiedStat(StatType type, float baseValue)
    {
        float modifiedValue = baseValue;
        // Add flat modifiers
        modifiedValue += activeTemporaryModifiers.Where(m => m.statType == type /* && m.applicationType == ModifierApplicationType.Flat */).Sum(m => m.value);
        
        // Apply percentage modifiers (example for additive percentage)
        // float percentBonus = activeTemporaryModifiers.Where(m => m.statType == type && m.applicationType == ModifierApplicationType.PercentAdd).Sum(m => m.value);
        // modifiedValue *= (1f + percentBonus);
        
        // Apply multiplicative percentage modifiers
        // foreach(var mod in activeTemporaryModifiers.Where(m => m.statType == type && m.applicationType == ModifierApplicationType.PercentMult))
        // {
        //    modifiedValue *= (1f + mod.value);
        // }
        return modifiedValue;
    }

    public int GetAttackPower() => Mathf.RoundToInt(GetModifiedStat(StatType.AttackPower, Stats.attackPower));
    public int GetDefense() => Mathf.RoundToInt(GetModifiedStat(StatType.Defense, Stats.defense));
    public int GetMagicPower() => Mathf.RoundToInt(GetModifiedStat(StatType.MagicPower, Stats.magicPower));
    public int GetMagicResistance() => Mathf.RoundToInt(GetModifiedStat(StatType.MagicResistance, Stats.magicResistance));
    // Corrected line below:
    public float GetCritChance() => GetModifiedStat(StatType.CritChance, Stats.criticalChance / 100f); // Assuming criticalChance is stored as a whole number percentage e.g. 5 for 5%
    public int GetSpeed() => Mathf.RoundToInt(GetModifiedStat(StatType.Speed, Stats.speed));
    public int GetMaxHealth() => Mathf.RoundToInt(GetModifiedStat(StatType.MaxHealth, Stats.maxHealth));


    // --- Combat Actions ---
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        int oldHealth = _currentHealth;
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(0, _currentHealth); // Ensure health doesn't go below 0

        Debug.Log($"[CHARACTER] {GetName()} received {amount} damage. Health: {oldHealth} -> {_currentHealth}");
        // OnHealthChanged?.Invoke(_currentHealth, GetMaxHealth()); // Update UI or other systems

        if (_currentHealth <= 0 && oldHealth > 0) 
        {
            Die();
        }
    }

    public void HealDamage(int amount)
    {
        if (!IsAlive && !(stateMachine.CurrentState is DeathState)) // Can't heal if truly gone, but can if in DeathState (for revives)
        {
            // If you want to prevent healing while in DeathState unless it's a revive, add more checks.
            // For now, allow healing if in DeathState, assuming it might be part of a revive.
            // if (!(stateMachine.CurrentState is DeathState)) return;
        }
        if (amount <= 0) return;

        int oldHealth = _currentHealth;
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, GetMaxHealth()); // Clamp to max health

        Debug.Log($"[CHARACTER] {GetName()} healed {amount}. Health: {oldHealth} -> {_currentHealth}");
        // OnHealthChanged?.Invoke(_currentHealth, GetMaxHealth());

        // If character was dead and is now healed above 0, transition out of DeathState
        if (oldHealth <= 0 && _currentHealth > 0 && stateMachine.CurrentState is DeathState)
        {
            Debug.Log($"[CHARACTER] {GetName()} revived! Transitioning from DeathState to IdleState.");
            stateMachine.ChangeState(new IdleState(this)); // Or a specific "RevivedState"
        }
    }
    
    public void ApplyDefenseStance()
    {
        if (!IsAlive) return;
        // Example: Add a temporary defense buff
        // This is a placeholder. You'd define the buff amount/duration properly.
        AddTemporaryModifier(new TemporaryModifier { 
            statType = StatType.Defense, 
            value = Stats.defense * 0.5f, // e.g., 50% defense buff
            duration = 1, // Lasts for 1 turn (you'll need to manage turn duration)
            isBuff = true,
            sourceName = "Defend Action"
        });
        Debug.Log($"[CHARACTER] {GetName()} applied defense stance.");
        // The DefendState itself handles animation and completion.
    }

    public void AddTemporaryModifier(TemporaryModifier modifier)
    {
        if (!IsAlive && !(stateMachine.CurrentState is DeathState && modifier.statType == StatType.MaxHealth)) // Allow MaxHealth changes on dead for revive logic
        {
             // Generally, don't apply modifiers to fully dead characters unless it's part of a revive mechanic
            if(!(stateMachine.CurrentState is DeathState && (modifier.statType == StatType.MaxHealth || modifier.statType == StatType.Health))) {
                 Debug.LogWarning($"[CHARACTER] Attempted to add modifier '{modifier.sourceName}' to {GetName()} but character is dead and not revivable by this stat. Modifiers not applied.");
                 return;
            }
        }
        activeTemporaryModifiers.Add(modifier);
        Debug.Log($"[CHARACTER] {GetName()} received modifier: {modifier.statType} {modifier.value} for {modifier.duration} turns from {modifier.sourceName}.");
        // Recalculate stats or notify UI if needed
        // Example: if (modifier.statType == StatType.MaxHealth) UpdateHealthDisplay();
    }

    // Call this at the start of each character's turn, or end of round
    public void TickTemporaryModifiers()
    {
        for (int i = activeTemporaryModifiers.Count - 1; i >= 0; i--)
        {
            activeTemporaryModifiers[i].duration--;
            if (activeTemporaryModifiers[i].duration <= 0)
            {
                Debug.Log($"[CHARACTER] {GetName()} modifier {activeTemporaryModifiers[i].statType} from {activeTemporaryModifiers[i].sourceName} expired.");
                activeTemporaryModifiers.RemoveAt(i);
                // Recalculate stats or notify UI
            }
        }
    }


    public virtual void Die()
    {
        bool wasAlreadyDeadOrDying = _currentHealth <= 0 || stateMachine.CurrentState is DeathState;
        _currentHealth = 0; 

        if (wasAlreadyDeadOrDying && stateMachine.CurrentState is DeathState)
        {
            // Debug.Log($"<color=grey>[CHARACTER] {GetName()} Die() called, but already dead/dying or in DeathState. Current Health: {_currentHealth}</color>");
            return;
        }
        
        Debug.Log($"<color=black>[CHARACTER] {GetName()} has died. Changing to DeathState. Current Health: {_currentHealth}</color>");
        stateMachine.ChangeState(new DeathState(this)); 
    }
}
