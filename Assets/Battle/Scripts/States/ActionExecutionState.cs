using UnityEngine;
using System;

public class ActionExecutionState : BattleState
{
    private BattleAction actionToExecute;
    private bool combatSystemCallbackReceived = false;

    public ActionExecutionState(BattleManager manager, BattleAction action) : base(manager)
    {
        this.actionToExecute = action;
    }

    public override void Enter()
    {
        Debug.Log($"<color=yellow>[BATTLE_EXEC_STATE] Entering. Executing: {actionToExecute.ActionType} by {actionToExecute.Actor.GetName()} on {actionToExecute.Target?.GetName() ?? "self"}</color>");
        combatSystemCallbackReceived = false;

        if (actionToExecute.Actor == null || !actionToExecute.Actor.IsAlive())
        {
            Debug.LogWarning($"<color=yellow>[BATTLE_EXEC_STATE] Actor {actionToExecute.Actor?.GetName()} is null or not alive. Skipping action.</color>");
            Complete(BattleEvent.ActionFullyComplete); // Signal completion to move on
            return;
        }
        
        // If target is required and not alive (and not self-target action)
        if (actionToExecute.Target != null && !actionToExecute.Target.IsAlive() && actionToExecute.ActionType != ActionType.Defend) // Defend doesn't need a live target
        {
            Debug.LogWarning($"<color=yellow>[BATTLE_EXEC_STATE] Target {actionToExecute.Target.GetName()} is not alive. Skipping action.</color>");
            Complete(BattleEvent.ActionFullyComplete); // Signal completion to move on
            return;
        }

        battleManager.GetCombatSystem().ExecuteAction(actionToExecute, () => {
            Debug.Log($"<color=green>[BATTLE_EXEC_STATE] CombatSystem reported action complete for {actionToExecute.Actor.GetName()}.</color>");
            combatSystemCallbackReceived = true;
        });
    }

    public override void Update()
    {
        if (combatSystemCallbackReceived)
        {
            Debug.Log($"<color=yellow>[BATTLE_EXEC_STATE] Combat system callback received. Completing state.</color>");
            Complete(BattleEvent.ActionFullyComplete);
        }
        // Optional: Add a timeout here if CombatSystem might get stuck
    }

    public override void Exit()
    {
        Debug.Log($"<color=yellow>[BATTLE_EXEC_STATE] Exiting.</color>");
    }
}