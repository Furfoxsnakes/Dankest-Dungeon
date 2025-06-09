using UnityEngine;

public class ItemState : CharacterState
{
    private Character target; 
    private const string ITEM_ANIMATION_TRIGGER = "Item";

    public ItemState(Character character, Character target) : base(character)
    {
        this.target = target;
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[STATE] {owner.GetName()} entering ItemState (targeting: {target?.GetName() ?? "self/area"}).");

        if (owner.characterAnimator != null)
        {
            owner.characterAnimator.SetTrigger(ITEM_ANIMATION_TRIGGER);
            Debug.Log($"[STATE] {owner.GetName()} triggered '{ITEM_ANIMATION_TRIGGER}' animation.");

            owner.RegisterInternalAnimationCallback(() => {
                Debug.Log($"<color=green>[STATE] {owner.GetName()} ItemUse animation complete (via callback). Completing with event.</color>");
                Complete(CharacterEvent.ItemComplete);
            });
        }
        else
        {
            Debug.LogWarning($"[STATE] {owner.GetName()} in ItemState has no Animator. Completing with event immediately.");
            Complete(CharacterEvent.ItemComplete);
        }
    }

    public override void Exit()
    {
        base.Exit();
        owner.ClearInternalAnimationCallback();
        Debug.Log($"[STATE] {owner.GetName()} exiting ItemState.");
    }
}