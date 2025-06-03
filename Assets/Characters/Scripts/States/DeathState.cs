using UnityEngine;

public class DeathState : CharacterState
{
    public DeathState(Character character) : base(character) { }
    
    public override void Enter()
    {
        // Register completion callback using single-parameter approach
        owner.RegisterAnimationCallback(FinishDeathAnimation);
        
        // Play death animation directly
        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.SetTrigger("Death");
        }
        else
        {
            FinishDeathAnimation();
        }
        
        Debug.Log($"{owner.GetName()} has died.");
    }
    
    private void FinishDeathAnimation()
    {
        // Death is a terminal state, so we don't actually complete it
        Debug.Log($"{owner.GetName()} death animation completed");
        
        // Here you could add visual effects like fading out the sprite
        // or other post-death processing
    }
    
    public override void Exit()
    {
        // Clean up callback to avoid memory leaks
        owner.RegisterAnimationCallback(null);
    }
}