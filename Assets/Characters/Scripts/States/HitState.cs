using UnityEngine;

public class HitState : CharacterState
{
    private const string HIT_ANIMATION = "Hit";

    // Constructor no longer needs damageAmount
    public HitState(Character character) : base(character)
    {
    }

    public override void Enter()
    {
        Debug.Log($"[HIT_STATE] {owner.GetName()} entering HitState.");
        owner.PlayAnimation(Character.AnimationType.Hit);

        // Register a callback for THIS state.
        owner.RegisterAnimationCallback(() => {
            Debug.Log($"[HIT_STATE] {owner.GetName()}'s hit animation finished. Completing state.");
            Complete(CharacterEvent.HitComplete);
        });
        
        // Damage application and health checks are removed from here.
        // CombatSystem handles damage after this animation.
        // Death checks will occur after CombatSystem applies damage.

        owner.characterAnimator.Play(HIT_ANIMATION);
    }

    // OnHitAnimationComplete is removed as the callback in Enter() handles completion.

    public override void Exit()
    {
        Debug.Log($"[HIT_STATE] {owner.GetName()} exiting HitState.");
        owner.RegisterAnimationCallback(null); // Clean up the callback this state registered.
    }
}