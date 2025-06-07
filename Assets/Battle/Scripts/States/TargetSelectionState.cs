using UnityEngine;
using DankestDungeon.Skills; // For SkillDefinitionSO and SkillTargetType
using System.Collections.Generic;
using System.Linq;

public class TargetSelectionState : BattleState
{
    private BattleAction actionInProgress;
    private List<Character> potentialTargets;
    private int currentTargetIndex = -1;
    private BattleUI battleUI;

    public TargetSelectionState(BattleManager manager, BattleAction action) : base(manager)
    {
        this.actionInProgress = action;
        this.battleUI = manager.GetBattleUI();
    }

    public override void Enter()
    {
        Debug.Log($"<color=yellow>Target Selection: Enter. Skill: {actionInProgress.UsedSkill.skillNameKey} by {actionInProgress.Actor.GetName()}</color>");
        
        potentialTargets = DeterminePotentialTargets();

        if (potentialTargets.Count == 0)
        {
            Debug.LogWarning("No valid targets found for selection. Cancelling.");
            if (battleUI != null) battleUI.HideTargetIndicator();
            Complete(BattleEvent.TargetSelectionCancelled, null);
            return;
        }

        currentTargetIndex = 0;
        UpdateTargetIndicator();

        // Subscribe to new static input events
        InputManager.UINavigatePerformed += HandleTargetSwitchEvent;
        InputManager.UIConfirmPerformed += HandleTargetConfirmEvent;
        InputManager.UICancelPerformed += HandleCancelEvent;
        
        // Ensure the UI action map is active if your InputManager manages this
        // InputManager.Instance?.EnableUIControls(); // Or a specific battle controls method
    }

    private List<Character> DeterminePotentialTargets()
    {
        List<Character> targets = new List<Character>();
        SkillDefinitionSO skill = actionInProgress.UsedSkill;
        Character actor = actionInProgress.Actor;
        FormationManager fm = battleManager.GetFormationManager();

        // Check actor's launch position first
        int actorRank = fm.GetCharacterRank(actor);
        if (skill.launchPositions == null || !skill.launchPositions.Contains(actorRank))
        {
            Debug.LogWarning($"Actor {actor.GetName()} at rank {actorRank} cannot use skill {skill.skillNameKey} from this position. Valid launch positions: {(skill.launchPositions != null ? string.Join(",", skill.launchPositions) : "NOT SET")}");
            return targets; // Empty list
        }

        List<Character> possible;
        switch (skill.targetType)
        {
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.EnemyRow:
                possible = battleManager.GetEnemyCharacters();
                break;
            case SkillTargetType.SingleAlly:
            case SkillTargetType.AllyRow:
                possible = battleManager.GetPlayerCharacters();
                break;
            default:
                Debug.LogError($"TargetSelectionState entered with unhandled target type: {skill.targetType}");
                return targets;
        }

        foreach (Character pTarget in possible)
        {
            if (pTarget.IsAlive)
            {
                int targetRank = fm.GetCharacterRank(pTarget);
                if (skill.targetPositions != null && skill.targetPositions.Contains(targetRank))
                {
                    if (skill.targetType == SkillTargetType.SingleAlly && pTarget == actor)
                    {
                        // If SingleAlly skills should NEVER target self, this is correct.
                        // If some CAN, you'll need a flag on the SkillDefinitionSO or SkillEffectData.
                        continue;
                    }
                    targets.Add(pTarget);
                }
            }
        }
        return targets;
    }

    // Updated handler for the new static event
    private void HandleTargetSwitchEvent(Vector2 direction)
    {
        if (potentialTargets.Count <= 1) return;

        int change = 0;
        // Prioritize horizontal navigation, then vertical if no horizontal input
        if (Mathf.Abs(direction.x) > 0.5f)
        {
            change = (int)Mathf.Sign(direction.x);
        }
        else if (Mathf.Abs(direction.y) > 0.5f) // Use for up/down if your ranks are visually vertical
        {
            change = -(int)Mathf.Sign(direction.y); // Invert Y if ranks are 1 (top) to 4 (bottom)
        }


        if (change != 0)
        {
            currentTargetIndex = (currentTargetIndex + change + potentialTargets.Count) % potentialTargets.Count;
            UpdateTargetIndicator();
        }
    }

    // Updated handler for the new static event
    private void HandleTargetConfirmEvent()
    {
        if (currentTargetIndex >= 0 && currentTargetIndex < potentialTargets.Count)
        {
            Character confirmedTarget = potentialTargets[currentTargetIndex];
            actionInProgress.SetTarget(confirmedTarget);
            Debug.Log($"<color=yellow>Target Confirmed: {confirmedTarget.GetName()} for skill {actionInProgress.UsedSkill.skillNameKey}</color>");
            if (battleUI != null) battleUI.HideTargetIndicator();
            Complete(BattleEvent.TargetSelected, actionInProgress);
        }
    }

    // Updated handler for the new static event
    private void HandleCancelEvent()
    {
        Debug.Log("<color=yellow>Target Selection Cancelled by player.</color>");
        if (battleUI != null) battleUI.HideTargetIndicator();
        Complete(BattleEvent.TargetSelectionCancelled, null);
    }

    private void UpdateTargetIndicator()
    {
        if (battleUI != null && currentTargetIndex >= 0 && currentTargetIndex < potentialTargets.Count)
        {
            battleUI.ShowTargetIndicator(potentialTargets[currentTargetIndex]);
        }
    }

    public override void Exit()
    {
        Debug.Log("<color=yellow>Target Selection: Exit</color>");
        // Unsubscribe from static input events
        InputManager.UINavigatePerformed -= HandleTargetSwitchEvent;
        InputManager.UIConfirmPerformed -= HandleTargetConfirmEvent;
        InputManager.UICancelPerformed -= HandleCancelEvent;

        if (battleUI != null)
        {
            battleUI.HideTargetIndicator();
            battleUI.ShowActionButtons(false); // Hide action buttons if they were shown
        }

        // Consider which control scheme should be active after exiting target selection
        // InputManager.Instance?.EnablePlayerControls(); // Or back to a general battle state control scheme
    }
}