using UnityEngine;

public class VictoryState : BattleState
{
    // This flag is less critical now if Update does nothing, but can be kept.
    private bool victoryMessageDisplayed = false; 
    
    public VictoryState(BattleManager manager) : base(manager) { }
    
    public override void Enter()
    {
        // Log victory once when entering the state.
        if (!victoryMessageDisplayed)
        {
            Debug.Log("<color=green>[BATTLE] Victory! All enemies defeated. Battle ended.</color>");
            victoryMessageDisplayed = true;
        }
        
        // For now, we will not process rewards or complete the state.
        // This will keep the BattleManager in this state until you manually
        // trigger a scene change or another action later.
        
        // Future TODO:
        // - Show victory UI panel
        // - Calculate and display rewards (gold, experience, items)
        // - Allow player to click "Continue" to trigger a return to map/town
    }
    
    public override void Update()
    {
        // Currently, do nothing in Update. 
        // The battle is won and effectively paused here.
        // We are not calling ProcessVictoryResults() or Complete().
        
        // if (!resultsProcessed) // Old logic, commented out
        // {
        //     ProcessVictoryResults();
        //     resultsProcessed = true;
        //     Complete(BattleEvent.VictoryProcessed);
        // }
    }
    
    // These methods can remain for future use, but won't be called for now.
    private void ProcessVictoryResults()
    {
        int goldReward = CalculateGoldReward();
        int expReward = CalculateExperienceReward();
        Debug.Log($"Earned {goldReward} gold and {expReward} experience!");
    }
    
    private int CalculateGoldReward()
    {
        // Ensure GetEnemyCharacters() is safe to call
        var enemies = battleManager.GetEnemyCharacters();
        return Random.Range(10, 51) * (enemies?.Count ?? 0);
    }
    
    private int CalculateExperienceReward()
    {
        var enemies = battleManager.GetEnemyCharacters();
        return Random.Range(20, 101) * (enemies?.Count ?? 0);
    }
}