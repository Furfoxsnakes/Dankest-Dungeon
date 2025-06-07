using UnityEngine;
using DankestDungeon.Skills;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class ActionExecutionState : BattleState
{
    private BattleAction currentAction;
    private CombatSystem combatSystem;
    private bool _executionComplete = false;

    public ActionExecutionState(BattleManager manager, BattleAction action) : base(manager)
    {
        this.currentAction = action;
        this.combatSystem = manager.GetCombatSystem();
        if (combatSystem == null) Debug.LogError("CombatSystem not found for ActionExecutionState.");
    }

    public override void Enter()
    {
        _executionComplete = false;
        Debug.Log($"<color=green>Action Execution: Enter. Actor: {currentAction.Actor.GetName()}, ActionType: {currentAction.ActionType}, Skill: {currentAction.UsedSkill?.skillNameKey}</color>");

        if (currentAction.Actor == null || !currentAction.Actor.IsAlive)
        {
            Debug.LogWarning($"Actor {currentAction.Actor?.GetName()} is null or not alive. Skipping action.");
            _executionComplete = true;
            return;
        }
        
        // Delegate to CombatSystem for all action execution
        combatSystem.ExecuteAction(currentAction, OnActionComplete);
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