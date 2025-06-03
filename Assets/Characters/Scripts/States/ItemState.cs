using UnityEngine;

public class ItemState : CharacterState
{
    private Character target; // Target of the item, can be null
    private const string ITEM_ANIMATION = "Item"; // Ensure this matches your Animator state name for item use

    public ItemState(Character character, Character target) : base(character)
    {
        this.target = target;
    }

    public override void Enter()
    {
        Debug.Log($"[ITEM_STATE] {owner.GetName()} entering ItemState (targeting: {target?.GetName() ?? "self/area"}).");
        owner.PlayAnimation(Character.AnimationType.Item);

        owner.RegisterAnimationCallback(() => {
            Debug.Log($"[ITEM_STATE] {owner.GetName()}'s item use animation finished. Completing state.");
            Complete(CharacterEvent.ItemComplete);
        });

        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.Play(ITEM_ANIMATION);
        }
        else
        {
            Debug.LogWarning($"[ITEM_STATE] {owner.GetName()} has no animator. Completing item state immediately.");
            Complete(CharacterEvent.ItemComplete);
        }
        // Applying item effects is moved to CombatSystem after this animation.
    }

    public override void Exit()
    {
        Debug.Log($"[ITEM_STATE] {owner.GetName()} exiting ItemState.");
        owner.RegisterAnimationCallback(null);
    }
}