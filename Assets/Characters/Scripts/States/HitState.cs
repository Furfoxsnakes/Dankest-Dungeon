using UnityEngine;

public class HitState : CharacterState
{
    private const string HIT_ANIMATION_TRIGGER = "Hit"; // This might be handled by Character.TakeHit()

    public HitState(Character character) : base(character) { }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entering HitState.");

        if (owner.characterAnimator != null)
        {
            // The actual animation trigger is likely handled by Character.TakeHit()
            // owner.characterAnimator.SetTrigger(HIT_ANIMATION_TRIGGER);
            // Debug.Log($"[STATE] {owner.GetName()} triggered '{HIT_ANIMATION_TRIGGER}' animation.");
            
            owner.RegisterInternalAnimationCallback(() => { // Changed to Internal
                Debug.Log($"<color=green>[STATE] {owner.GetName()} Hit animation complete (via INTERNAL callback). Completing with event.</color>");
                Complete(CharacterEvent.HitComplete); 
            });
            // Note: Character.TakeHit() is called by ActionSequenceHandler, which then waits for an *external* callback.
            // This internal callback is for the HitState's own lifecycle.
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
        owner.ClearInternalAnimationCallback(); // Changed to Internal
        Debug.Log($"[STATE] {owner.GetName()} exiting HitState.");
    }
}