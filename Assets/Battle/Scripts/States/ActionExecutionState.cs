using UnityEngine;
using DankestDungeon.Skills;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DankestDungeon.Characters; // Assuming Player class is in this namespace

public class ActionExecutionState : BattleState
{
    private BattleAction currentAction; // This is the correct field
    private CombatSystem combatSystem;
    private bool _executionComplete = false;

    public ActionExecutionState(BattleManager manager, BattleAction action) : base(manager)
    {
        this.currentAction = action;
        this.combatSystem = manager.GetCombatSystem();
        if (combatSystem == null) Debug.LogError("CombatSystem not found for ActionExecutionState.");
        if (this.currentAction == null) Debug.LogError("ActionExecutionState initialized with a null action.");
    }

    public override void Enter()
    {
        Debug.Log("<color=orange>Action Execution: Enter</color>");
        if (currentAction == null) // Use currentAction here
        {
            Debug.LogError("ActionExecutionState entered with no action to execute (currentAction is null).");
            Complete(BattleEvent.ActionFullyComplete, null); // Or some error event
            return;
        }

        Debug.Log($"Executing action: {(currentAction.ActionType == ActionType.Skill ? currentAction.UsedSkill?.skillNameKey : currentAction.ActionType.ToString())} by {currentAction.Actor?.GetName()} on {currentAction.Target?.GetName() ?? "N/A"}");

        ActionSequenceHandler ash = battleManager.GetActionSequenceHandler();
        if (ash != null)
        {
            ash.ProcessAction(currentAction, () => // Use currentAction here
            {
                // Determine the correct event based on whose action it was
                // Ensure currentAction and currentAction.Actor are not null before accessing GetComponent
                BattleEvent completionEvent = BattleEvent.ActionFullyComplete; // Default event
                if (currentAction != null && currentAction.Actor != null)
                {
                    // Ensure Hero class is known here (either via using directive or fully qualified name)
                    completionEvent = (currentAction.Actor.GetComponent<Hero>() != null) ? BattleEvent.ActionFullyComplete : BattleEvent.EnemyActionComplete;
                }
                else
                {
                    Debug.LogWarning("currentAction or currentAction.Actor is null when determining completion event.");
                }
                Complete(completionEvent, currentAction); // Use currentAction here
            });
        }
        else
        {
            Debug.LogError("ActionSequenceHandler is null in ActionExecutionState. Cannot process action.");
            // Decide how to complete if ASH is null. Maybe an error event or just skip.
            Complete(BattleEvent.ActionFullyComplete, currentAction); // Or pass null if action couldn't be processed
        }
    }

    private void OnActionComplete()
    {
        Debug.Log("<color=green>Action execution completed</color>");
        _executionComplete = true;
    }

    public override void Update()
    {
        if (_executionComplete)
        {
            Complete(BattleEvent.ActionFullyComplete, null);
        }
    }

    public override void Exit()
    {
        Debug.Log("<color=green>Action Execution: Exit</color>");
    }
}