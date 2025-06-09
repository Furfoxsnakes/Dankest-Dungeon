using UnityEngine;

public class AttackState : CharacterState
{
    private Character target;

    public AttackState(Character character, Character target) : base(character)
    {
        this.target = target;
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entered AttackState. Target: {target?.GetName() ?? "None"}");

        if (owner.characterAnimator != null)
        {
            owner.RegisterInternalAnimationCallback(() => { // Changed to Internal
                Debug.Log($"<color=green>[STATE] {owner.GetName()} Attack animation complete (via INTERNAL callback). Completing with event.</color>");
                Complete(CharacterEvent.AttackComplete);
            });
            // Note: The actual playing of the animation (e.g., owner.Attack(target) or owner.PlayAnimation(Attack))
            // is now expected to be triggered by ActionSequenceHandler, which then waits for an *external* callback.
            // This internal callback is for the AttackState's own lifecycle if it needs to react to its animation ending.
        }
        else
        {
            Debug.LogWarning($"[STATE] {owner.GetName()} in AttackState has no Animator. Completing with event immediately.");
            Complete(CharacterEvent.AttackComplete);
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.ClearInternalAnimationCallback(); // Changed to Internal
        Debug.Log($"[STATE] {owner.GetName()} exited AttackState.");
    }

    public override void Update()
    {
        base.Update();
    }
}