using UnityEngine;

public class DefendState : CharacterState
{
    private const string DEFEND_ANIMATION_TRIGGER = "Defend";

    public DefendState(Character character) : base(character) { }
    
    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entering DefendState.");
        
        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.SetTrigger(DEFEND_ANIMATION_TRIGGER);
            Debug.Log($"[STATE] {owner.GetName()} triggered '{DEFEND_ANIMATION_TRIGGER}' animation.");

            owner.RegisterInternalAnimationCallback(() => {
                Debug.Log($"<color=green>[STATE] {owner.GetName()} Defend animation complete (via callback). Completing with event.</color>");
                Complete(CharacterEvent.DefendComplete);
            });
        }
        else
        {
            Debug.LogWarning($"[STATE] {owner.GetName()} in DefendState has no Animator. Completing with event immediately.");
            Complete(CharacterEvent.DefendComplete);
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.ClearInternalAnimationCallback(); 
        Debug.Log($"[STATE] {owner.GetName()} exiting DefendState.");
    }
}