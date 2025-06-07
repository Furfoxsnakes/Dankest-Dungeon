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
            owner.characterAnimator.SetTrigger("Attack");
            Debug.Log($"[STATE] {owner.GetName()} triggered 'Attack' animation.");

            owner.RegisterAnimationCallback(() => {
                Debug.Log($"<color=green>[STATE] {owner.GetName()} Attack animation complete (via callback). Completing with event.</color>");
                Complete(CharacterEvent.AttackComplete);
            });
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
        owner.ClearAnimationCallback(); 
        Debug.Log($"[STATE] {owner.GetName()} exited AttackState.");
    }

    public override void Update()
    {
        base.Update();
    }
}