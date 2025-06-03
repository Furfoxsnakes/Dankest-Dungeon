using UnityEngine;

public class AttackState : CharacterState
{
    private Character target; // Still useful for aiming or specific attack animations
    private const string ATTACK_ANIMATION = "Attack";

    public AttackState(Character character, Character target) : base(character)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log($"[ATTACK_STATE] {owner.GetName()} entering AttackState, targeting {target.GetName()}.");
        owner.PlayAnimation(Character.AnimationType.Attack);

        // Register a callback for THIS state to know when ITS animation is done.
        // This is separate from the CombatSystem's callback.
        owner.RegisterAnimationCallback(() => {
            Debug.Log($"[ATTACK_STATE] {owner.GetName()}'s attack animation finished. Completing state.");
            Complete(CharacterEvent.AttackComplete);
        });

        owner.characterAnimator.Play(ATTACK_ANIMATION);
    }

    // ApplyDamage method is removed from here. CombatSystem handles it.

    public override void Exit()
    {
        Debug.Log($"[ATTACK_STATE] {owner.GetName()} exiting AttackState.");
        owner.RegisterAnimationCallback(null); // Clean up the callback this state registered.
    }
}