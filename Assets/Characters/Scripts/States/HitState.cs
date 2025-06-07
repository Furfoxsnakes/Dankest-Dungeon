using UnityEngine;

public class HitState : CharacterState
{
    private const string HIT_ANIMATION_TRIGGER = "Hit";

    public HitState(Character character) : base(character) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entering HitState.");

        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.SetTrigger(HIT_ANIMATION_TRIGGER);
            Debug.Log($"[STATE] {owner.GetName()} triggered '{HIT_ANIMATION_TRIGGER}' animation.");
            
            owner.RegisterAnimationCallback(() => {
                Debug.Log($"<color=green>[STATE] {owner.GetName()} Hit animation complete (via callback). Completing with event.</color>");
                // Here, you might also check if the hit was fatal.
                // If owner.IsAlive is false after damage application (which happens outside this state),
                // the CombatSystem or Character.Die() would handle the DeathState transition.
                // This state just signals its animation is done.
                Complete(CharacterEvent.HitComplete); 
            });
        }
        else
        {
            Debug.LogWarning($"[STATE] {owner.GetName()} in HitState has no Animator. Completing with event immediately.");
            Complete(CharacterEvent.HitComplete);
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.ClearAnimationCallback(); 
        Debug.Log($"[STATE] {owner.GetName()} exiting HitState.");
    }
}