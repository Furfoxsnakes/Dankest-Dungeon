using UnityEngine;

public class PlayerTurnState : BattleState
{
    private bool actionSelected = false;
    private Character activeCharacter;
    private BattleUI battleUI;
    
    public PlayerTurnState(BattleManager manager) : base(manager) 
    {
        // Get reference from the battle manager instead of finding it
        battleUI = manager.GetBattleUI();
    }
    
    public override void Enter()
    {
        Debug.Log("Entering Player Turn state");
        actionSelected = false;
        
        // Get the current active character
        activeCharacter = battleManager.GetTurnSystem().GetCurrentActor();
        
        // Show the indicator above the active character
        if (battleUI != null)
        {
            battleUI.ShowActiveCharacterIndicator(activeCharacter);
        }
        
        // Set up input handling
        if (InputManager.Instance != null)
        {
            InputManager.OnSubmitPerformed += OnSubmitPerformed;
            InputManager.Instance.EnableBattleActions();
        }
        else
        {
            Debug.LogError("InputManager singleton not found in scene");
        }
    }
    
    private void OnSubmitPerformed()
    {
        // Hide the indicator when action is selected
        if (battleUI != null)
        {
            battleUI.HideActiveCharacterIndicator();
        }
        
        Debug.Log("Submit action performed");
        actionSelected = true;
    }
    
    public override void Exit()
    {
        // Ensure indicator is hidden when leaving this state
        if (battleUI != null)
        {
            battleUI.HideActiveCharacterIndicator();
        }
        
        // Clean up event subscriptions
        InputManager.OnSubmitPerformed -= OnSubmitPerformed;
        
        if (InputManager.Instance != null)
        {
            InputManager.Instance.DisableBattleActions();
        }
    }
    
    public override void Update()
    {
        // Check if player has selected an action
        if (actionSelected)
        {
            Complete(BattleEvent.PlayerActionSelected);
        }
    }
}