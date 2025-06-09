using UnityEngine;

public class DeathState : CharacterState
{
    private const string DEATH_ANIMATION_TRIGGER = "Death"; // This might be handled by Character.Die() or similar

    public DeathState(Character character) : base(character) { }
    
    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entered DeathState.");
        
        if (owner.characterAnimator != null)
        {
            // The actual animation trigger is likely handled by a Character.Die() method
            // owner.characterAnimator.SetTrigger(DEATH_ANIMATION_TRIGGER);
            // Debug.Log($"[STATE] {owner.GetName()} triggered '{DEATH_ANIMATION_TRIGGER}' animation.");
            
            owner.RegisterInternalAnimationCallback(() => { // Changed to Internal
                Debug.Log($"<color=grey>[STATE] {owner.GetName()} Death animation 'completed' (via INTERNAL callback). Completing with DeathComplete event.</color>");
                Complete(CharacterEvent.DeathComplete); 
            });
        }
        else
        {
            Debug.LogWarning($"[STATE] {owner.GetName()} in DeathState has no Animator. Completing with DeathComplete event immediately.");
            Complete(CharacterEvent.DeathComplete);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        owner.ClearInternalAnimationCallback(); // Changed to Internal
        Debug.Log($"[STATE] {owner.GetName()} exiting DeathState (e.g., revived).");
    }

    public override void Update()
    {
        // Death state usually does nothing in Update.
    }
}