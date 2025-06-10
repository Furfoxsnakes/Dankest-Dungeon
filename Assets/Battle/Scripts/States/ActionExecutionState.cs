using UnityEngine;
using DankestDungeon.Skills;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using DankestDungeon.Characters; // Assuming Player class is in this namespace

public class ActionExecutionState : BattleState
{
    private BattleAction currentAction;
    private CombatSystem combatSystem;
    private ActionSequenceHandler actionSequenceHandler; // Store a reference

    private bool _ashInitialProcessingDone = false; // Flag to indicate ASH.ProcessAction's callback has fired
    private BattleEvent _completionEventToFire;     // Store the event to fire
    private BattleAction _actionDataForCompletion;  // Store the action data for the completion event

    public ActionExecutionState(BattleManager manager, BattleAction action) : base(manager)
    {
        this.currentAction = action;
        this.combatSystem = manager.GetCombatSystem();
        this.actionSequenceHandler = manager.GetActionSequenceHandler(); // Get ASH here

        if (combatSystem == null) Debug.LogError("CombatSystem not found for ActionExecutionState.");
        if (this.actionSequenceHandler == null) Debug.LogError("ActionSequenceHandler not found for ActionExecutionState.");
        if (this.currentAction == null) Debug.LogError("ActionExecutionState initialized with a null action.");
    }

    public override void Enter()
    {
        Debug.Log("<color=orange>Action Execution: Enter</color>");
        _ashInitialProcessingDone = false; // Reset flag

        if (currentAction == null)
        {
            Debug.LogError("ActionExecutionState entered with no action to execute (currentAction is null).");
            Complete(BattleEvent.ActionFullyComplete, null); 
            return;
        }
        
        if (actionSequenceHandler == null)
        {
            Debug.LogError("ActionSequenceHandler is null in ActionExecutionState. Cannot process action. Completing immediately.");
            _completionEventToFire = DetermineCompletionEvent(currentAction);
            _actionDataForCompletion = currentAction;
            Complete(_completionEventToFire, _actionDataForCompletion);
            return;
        }

        // Use PrimaryTarget for logging the initial target focus
        Debug.Log($"Executing action: {(currentAction.ActionType == ActionType.Skill ? currentAction.UsedSkill?.skillNameKey : currentAction.ActionType.ToString())} by {currentAction.Actor?.GetName()} on {currentAction.PrimaryTarget?.GetName() ?? "N/A / Area"}");

        actionSequenceHandler.ProcessAction(currentAction, () =>
        {
            Debug.Log($"[ActionExecutionState] ActionSequenceHandler has initially processed the action: {currentAction.ActionType}");
            
            _completionEventToFire = DetermineCompletionEvent(currentAction);
            _actionDataForCompletion = currentAction;
            _ashInitialProcessingDone = true; 
        });
    }

    private BattleEvent DetermineCompletionEvent(BattleAction action)
    {
        // Regardless of player or enemy, when an action's execution is finished,
        // it means the action is "fully complete" for this turn segment.
        // BattleManager will then use this to advance the turn.
        return BattleEvent.ActionFullyComplete;
    }

    // Removed OnActionComplete() as its role is now handled by Update() checking ASH.

    public override void Update()
    {
        // Only proceed to check ASH if its initial processing callback has fired.
        if (_ashInitialProcessingDone)
        {
            // Now, also check if the ActionSequenceHandler has no more active sequences.
            if (actionSequenceHandler != null && !actionSequenceHandler.IsSequenceRunning())
            {
                Debug.Log($"<color=green>Action execution fully completed (ASH sequences finished). Event: {_completionEventToFire}</color>");
                Complete(_completionEventToFire, _actionDataForCompletion);
                _ashInitialProcessingDone = false; // Reset for potential re-entry, though states usually are new instances
            }
            // Optional: Add a timeout here if worried about getting stuck
        }
    }

    public override void Exit()
    {
        Debug.Log("<color=green>Action Execution: Exit</color>");
    }
}