using UnityEngine;

public class IdleState : CharacterState
{
    public IdleState(Character character) : base(character) { }
    
    public override void Enter()
    {
        // Play idle animation
        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.Play("Idle");
            // Or use Play directly: owner.characterAnimator.Play("Idle");
        }
        
        Debug.Log($"{owner.GetName()} is now idle.");
    }
}