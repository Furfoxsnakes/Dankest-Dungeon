using UnityEngine;

public class VictoryState : BattleState
{
    private bool resultsProcessed = false;
    
    public VictoryState(BattleManager manager) : base(manager) { }
    
    public override void Enter()
    {
        Debug.Log("Victory! Battle won");
        resultsProcessed = false;
        
        // Show victory UI
        // Display rewards
        // Calculate experience
    }
    
    public override void Update()
    {
        if (!resultsProcessed)
        {
            // Process victory effects (animations, UI, etc.)
            ProcessVictoryResults();
            resultsProcessed = true;
            
            // Report completion
            Complete(BattleEvent.VictoryProcessed);
        }
    }
    
    private void ProcessVictoryResults()
    {
        // Example rewards processing
        int goldReward = CalculateGoldReward();
        int expReward = CalculateExperienceReward();
        
        Debug.Log($"Earned {goldReward} gold and {expReward} experience!");
        
        // In a real implementation, you'd assign these to the player's party
    }
    
    private int CalculateGoldReward()
    {
        // Simple example: 10-50 gold per enemy
        return Random.Range(10, 51) * battleManager.GetEnemyCharacters().Count;
    }
    
    private int CalculateExperienceReward()
    {
        // Simple example: 20-100 exp per enemy
        return Random.Range(20, 101) * battleManager.GetEnemyCharacters().Count;
    }
}