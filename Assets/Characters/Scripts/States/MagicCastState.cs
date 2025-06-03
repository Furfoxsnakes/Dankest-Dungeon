using UnityEngine;

public class MagicCastState : CharacterState
{
    private Character target; // Target might be used for animation targeting if needed
    // private string spellName; // Could be used if different spells have different animations
    private const string CAST_ANIMATION = "Magic"; // Ensure this matches your Animator state name for casting

    public MagicCastState(Character character, Character target) : base(character)
    {
        this.target = target;
        // this.spellName = spellName; // If you pass spell-specific info
    }
    
    public override void Enter()
    {
        Debug.Log($"[MAGIC_CAST_STATE] {owner.GetName()} entering MagicCastState (targeting: {target?.GetName() ?? "self/area"}).");
        owner.PlayAnimation(Character.AnimationType.Cast);
        
        // Register animation completion callback for this state
        owner.RegisterAnimationCallback(() => {
            Debug.Log($"[MAGIC_CAST_STATE] {owner.GetName()}'s cast animation finished. Completing state.");
            Complete(CharacterEvent.MagicComplete);
        });
        
        // Play magic animation directly
        if (owner.characterAnimator != null)
        {
            // Assuming "Magic" is a state name. Use SetTrigger if it's a trigger.
            owner.characterAnimator.Play(CAST_ANIMATION); 
        }
        else
        {
            Debug.LogWarning($"[MAGIC_CAST_STATE] {owner.GetName()} has no animator. Completing cast state immediately.");
            Complete(CharacterEvent.MagicComplete); // If no animator, immediately trigger completion
        }
        // Applying magic damage/effects is moved to CombatSystem after this animation.
    }
    
    public override void Exit()
    {
        Debug.Log($"[MAGIC_CAST_STATE] {owner.GetName()} exiting MagicCastState.");
        owner.RegisterAnimationCallback(null); // Clean up callback
    }

    // ApplyMagicDamage method is removed. CombatSystem handles effects.
    // FinishCasting method is effectively replaced by the lambda in RegisterAnimationCallback.
}