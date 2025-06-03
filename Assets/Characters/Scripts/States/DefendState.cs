using UnityEngine;

public class DefendState : CharacterState
{
    private const string DEFEND_ANIMATION = "Defend"; // Ensure this matches your Animator state name

    public DefendState(Character character) : base(character) { }
    
    public override void Enter()
    {
        Debug.Log($"[DEFEND_STATE] {owner.GetName()} entering DefendState.");
        owner.PlayAnimation(Character.AnimationType.Defend);

        // Register a callback for THIS state to know when ITS animation is done.
        owner.RegisterAnimationCallback(() => {
            Debug.Log($"[DEFEND_STATE] {owner.GetName()}'s defend animation finished. Completing state.");
            Complete(CharacterEvent.DefendComplete);
        });
        
        // Play defend animation directly
        if (owner.characterAnimator != null)
        {
            // Assuming "Defend" is a state name in your animator. Use SetTrigger if it's a trigger.
            owner.characterAnimator.Play(DEFEND_ANIMATION); 
        }
        else
        {
            Debug.LogWarning($"[DEFEND_STATE] {owner.GetName()} has no animator. Completing defend state immediately.");
            Complete(CharacterEvent.DefendComplete); // Complete immediately if no animator
        }
        // Applying defense stance is moved to CombatSystem after this animation.
    }

    public override void Exit()
    {
        Debug.Log($"[DEFEND_STATE] {owner.GetName()} exiting DefendState.");
        owner.RegisterAnimationCallback(null); // Clean up the callback this state registered.
    }
}