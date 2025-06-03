using UnityEngine;
using System;

public class Character : MonoBehaviour
{
    [SerializeField] private CharacterStats stats;
    [SerializeField] public Animator characterAnimator; // Keep public for state access
    
    [SerializeField] private bool debugStateTransitions = false;
    
    // Health management - make internal for states to access
    internal int _currentHealth; // Make internal so states can modify directly
    public int CurrentHealth => _currentHealth;

    public int FormationPosition { get; set; }
    
    // State machine for character states
    private StateMachine<CharacterState> stateMachine; // Assuming this exists
    
    public CharacterState GetCurrentState()
    {
        if (stateMachine != null)
        {
            return stateMachine.CurrentState;
        }
        return null; // Or a default state if appropriate
    }

    // Animation event callbacks
    private Action onAnimationComplete;
    private bool isAnimating = false;

    // Public access to stats
    public CharacterStats Stats => stats;

    public enum AnimationType { None, Attack, Hit, Cast, Defend, Item }

    private AnimationType currentAnimationType = AnimationType.None;

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
    }
    
    protected virtual void Start()
    {
        // Start in idle state
        stateMachine.Initialize(new IdleState(this));
    }
    
    protected virtual void Update()
    {
        // Update the current state
        stateMachine.Update();
        
        // Check for state completion and handle transitions based on events
        if (stateMachine.CheckStateCompletion(out object result) && result is CharacterEvent characterEvent)
        {
            HandleStateEvent(characterEvent);
        }
    }

    private void HandleStateEvent(CharacterEvent characterEvent)
    {
        switch (characterEvent)
        {
            case CharacterEvent.AttackComplete:
            case CharacterEvent.HitComplete:
            case CharacterEvent.DefendComplete:
            case CharacterEvent.MagicComplete:
            case CharacterEvent.ItemComplete: // Add this case
                stateMachine.ChangeState(new IdleState(this));
                break;

            case CharacterEvent.HitKilled:
                // Character died from getting hit
                stateMachine.ChangeState(new DeathState(this));
                break;

            case CharacterEvent.DeathComplete:
                // Character is now dead - might handle removal or other post-death effects
                // Usually a terminal state, so we don't transition
                break;

            default:
                Debug.LogWarning($"Unhandled character event: {characterEvent}");
                break;
        }
    }
    
    // Public methods to change states
    public void Attack(Character target)
    {
        // This remains the same, it triggers AttackState
        Debug.Log($"<color=blue>[CHARACTER] {GetName()} initiating attack on {target?.GetName()} via AttackState.</color>");
        stateMachine.ChangeState(new AttackState(this, target));
    }

    // This method is now called by CombatSystem to make the character play its hit animation.
    public void TakeHit()
    {
        if (!IsAlive())
        {
            Debug.LogWarning($"{GetName()} is not alive, cannot play hit animation.");
            // If CombatSystem relies on state changes, we might not need to call OnAnimationComplete here.
            // The character will remain in its current (likely Idle or Death) state.
            // CombatSystem's WaitUntil IdleState will proceed or skip based on IsAlive.
            return;
        }

        if (characterAnimator != null && characterAnimator.HasState(0, Animator.StringToHash("Hit")))
        {
            Debug.Log($"[CHARACTER] {GetName()} transitioning to HitState to play hit animation.");
            stateMachine.ChangeState(new HitState(this)); 
        }
        else
        {
            Debug.LogWarning($"[CHARACTER] {GetName()} doesn't have a Hit animation! Skipping hit animation and transitioning to Idle.");
            // If no hit animation, we assume it's an instant recovery to Idle for CombatSystem's WaitUntil.
            stateMachine.ChangeState(new IdleState(this)); 
        }
    }

    public void Defend()
    {
        Debug.Log($"<color=blue>[CHARACTER] {GetName()} initiating Defend via DefendState.</color>");
        stateMachine.ChangeState(new DefendState(this));
    }

    public void CastMagic(Character target) // Target can be null for self-casts/AoE
    {
        Debug.Log($"<color=blue>[CHARACTER] {GetName()} initiating magic cast on {target?.GetName() ?? "self/area"} via MagicCastState.</color>");
        stateMachine.ChangeState(new MagicCastState(this, target)); // Pass target to MagicCastState
    }

    public void UseItem(Character target) // Target can be null
    {
        Debug.Log($"<color=blue>[CHARACTER] {GetName()} initiating item use on {target?.GetName() ?? "self/area"} via ItemState.</color>");
        stateMachine.ChangeState(new ItemState(this, target)); // Create and use ItemState
    }

    // This method is now called by CombatSystem to apply damage AFTER animations.
    // It does NOT trigger the HitState.
    public void ReceiveDamage(int damageAmount)
    {
        if (!IsAlive()) return;

        _currentHealth -= damageAmount;
        _currentHealth = Mathf.Max(0, _currentHealth);

        Debug.Log($"[CHARACTER] {GetName()} received {damageAmount} damage. Current Health: {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Debug.Log($"[CHARACTER] {GetName()} has been defeated by damage.");
            // The DeathState transition will be handled by the state machine when it updates
            // or you can force it here if preferred, but HandleStateEvent should catch HitKilled/Death.
            // For now, let the normal state machine flow handle death if a state signals it.
            // If no state signals death, the BattleManager will need to check IsAlive().
            // Let's ensure the character itself transitions to DeathState.
            stateMachine.ChangeState(new DeathState(this));
        }
    }

    // Animation event registration
    public void RegisterAnimationCallback(Action onComplete)
    {
        onAnimationComplete = null;
        onAnimationComplete = onComplete;
        
         if (onComplete != null)
        {
            Debug.Log($"[CHARACTER] {GetName()} registered animation callback for CombatSystem.");
        }
    }
    
    // public void RegisterAnimationCallback(Action callback)
    // {
    //     onAnimationComplete = callback;
    //     Debug.Log($"<color=blue>[CHARACTER] {GetName()}: Animation callback registered: {(callback != null ? "Valid" : "NULL")}</color>");
    // }

    public void PlayAnimation(AnimationType type)
    {
        currentAnimationType = type;
        isAnimating = true;
        Debug.Log($"{GetName()} started {type} animation");
        
        // Note: We don't start the animation here - that's done by the state
        // This just tracks what animation is currently playing
    }

    // Called by CharacterVisuals
    public void OnAnimationComplete()
    {
        Debug.Log($"[CHARACTER] Animation completion signal received for {GetName()}. Current animation type: {currentAnimationType}");
        
        Action callbackToInvoke = onAnimationComplete;
        onAnimationComplete = null; // Consume the callback for CombatSystem

        if (callbackToInvoke != null)
        {
            Debug.Log($"[CHARACTER] Invoking registered CombatSystem callback for {GetName()}.");
            callbackToInvoke.Invoke();
        }
        else 
        {
            Debug.Log($"[CHARACTER] No CombatSystem callback was registered for {GetName()} or it was already consumed.");
        }
        
        // Note: The Character's own state machine (AttackState, HitState)
        // should register their *own* callbacks if they need to react to OnAnimationComplete
        // for their internal state transitions (e.g., AttackState -> Complete(CharacterEvent.AttackComplete)).
        // The current onAnimationComplete is primarily for the CombatSystem's sequencing.
    }
    
    // Utility methods
    public string GetName() => stats.characterName;
    
    public bool IsAlive() => _currentHealth > 0;
    
    public bool IsAnimating() => isAnimating;
    
    public void ApplyDefenseStance()
    {
        Debug.Log($"[CHARACTER] {GetName()} has defense stance applied.");
        // Implement defense buff logic here (e.g., modify a temporary defense stat)
    }
    
    public void ResetHealth()
    {
        _currentHealth = stats.maxHealth;
    }
    
    public int GetAttackPower()
    {
        // Start with base attack power
        int attackPower = stats.attackPower;
        
        // Apply any buffs/status effects
        
        return attackPower;
    }
    
    public int GetDefense()
    {
        // Start with base defense
        int defense = stats.defense;
        
        // Check for defense stance and other buffs
        
        return defense;
    }
    
    public int GetMagicPower()
    {
        // Get magic power from stats
        int magicPower = stats.magicPower;
        
        // Apply any buffs or status effects
        
        return magicPower;
    }
    
    public int GetMagicResistance()
    {
        // Get magic resistance from stats
        int magicResist = stats.magicResistance;
        
        // Apply any buffs or status effects
        
        return magicResist;
    }
    
    // Virtual method for customizing death behavior
    public virtual void Die()
    {
        Debug.Log($"{GetName()} has died.");
        
        // Common death behavior for all characters
        
        // Death state will be handled separately by the state machine
        // This method focuses on visual/gameplay aspects of death
    }
}