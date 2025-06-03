using UnityEngine;

public class EnemyTurnState : BattleState
{
    private float aiThinkingTime = 1.0f;
    private float timer = 0f;
    private bool actionPerformed = false;
    private BattleAction decidedAction;
    
    public EnemyTurnState(BattleManager manager) : base(manager) { }
    
    public override void Enter()
    {
        Debug.Log("Entering Enemy Turn state");
        timer = 0f;
        actionPerformed = false;
        decidedAction = null;
        
        // Get the enemy's action decision immediately
        PerformAIDecision();
    }
    
    public override void Update()
    {
        if (!actionPerformed)
        {
            timer += Time.deltaTime;
            
            if (timer >= aiThinkingTime)
            {
                actionPerformed = true;
                
                // Send the pre-decided action to the battle manager
                Complete(BattleEvent.EnemyActionComplete, decidedAction);
            }
        }
    }
    
    private void PerformAIDecision()
    {
        // Get the current enemy
        Character currentEnemy = battleManager.GetTurnSystem().GetCurrentActor();
        
        // Ensure it's actually an Enemy type
        if (currentEnemy is Enemy enemy)
        {
            // Get the AI decision from the enemy
            decidedAction = enemy.DecideAction(
                battleManager.GetEnemyCharacters(), 
                battleManager.GetPlayerCharacters()
            );
            
            Debug.Log($"{currentEnemy.GetName()} decided to {decidedAction.ActionType} targeting {decidedAction.Target.GetName()}");
        }
        else
        {
            // Fallback for non-enemy characters
            Debug.LogWarning("Current actor is not an Enemy!");
            
            // Create a default action - skip turn
            decidedAction = new BattleAction(
                currentEnemy, 
                currentEnemy, 
                ActionType.Skip
            );
        }
    }
}