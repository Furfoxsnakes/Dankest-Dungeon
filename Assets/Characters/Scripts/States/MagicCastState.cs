using UnityEngine;

public class MagicCastState : CharacterState
{
    private Character target; 
    private const string CAST_ANIMATION_TRIGGER = "Magic";

    public MagicCastState(Character character, Character target) : base(character)
    {
        this.target = target;
    }
    
    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entering MagicCastState (targeting: {target?.GetName() ?? "self/area"}).");
        
        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.SetTrigger(CAST_ANIMATION_TRIGGER); 
            Debug.Log($"[STATE] {owner.GetName()} triggered '{CAST_ANIMATION_TRIGGER}' animation.");

            owner.RegisterAnimationCallback(() => {
                Debug.Log($"<color=green>[STATE] {owner.GetName()} MagicCast animation complete (via callback). Completing with event.</color>");
                Complete(CharacterEvent.MagicComplete);
            });
        }
        else
        {
            Debug.LogWarning($"[STATE] {owner.GetName()} in MagicCastState has no Animator. Completing with event immediately.");
            Complete(CharacterEvent.MagicComplete);
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        owner.ClearAnimationCallback(); 
        Debug.Log($"[STATE] {owner.GetName()} exiting MagicCastState.");
    }
}