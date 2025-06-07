using UnityEngine;
using DankestDungeon.Skills; // For SkillDefinitionSO

public class PlayerTurnState : BattleState
{
    private bool skillHasBeenSelected = false; // Renamed for clarity
    private SkillDefinitionSO currentSelectedSkill;
    private Character activeCharacter;
    private BattleUI battleUI;

    public PlayerTurnState(BattleManager manager) : base(manager) 
    {
        battleUI = manager.GetBattleUI(); // battleUI should be cached by BattleManager
        if (battleUI == null) Debug.LogError("BattleUI not found for PlayerTurnState via BattleManager.");
    }
    
    public override void Enter()
    {
        Debug.Log("<color=cyan>Player Turn: Enter</color>");
        skillHasBeenSelected = false;
        currentSelectedSkill = null;
        activeCharacter = battleManager.GetTurnSystem().GetCurrentActor();

        if (activeCharacter == null || !activeCharacter.IsAlive)
        {
            Debug.LogWarning($"PlayerTurnState: Active character {activeCharacter?.GetName()} is null or not alive. Advancing turn.");
            Complete(BattleEvent.ActionFullyComplete, null); // Skip turn
            return;
        }

        if (battleUI != null)
        {
            battleUI.ShowActiveCharacterIndicator(activeCharacter);
            battleUI.ShowSkillButtons(activeCharacter, OnPlayerChoseSkill); // Pass the callback
        }
        else
        {
            Debug.LogError("BattleUI is null in PlayerTurnState.Enter. Cannot show skill buttons.");
            Complete(BattleEvent.ActionFullyComplete, null); // Skip turn if UI fails
        }
    }

    // Callback for when a skill button is pressed in the UI
    public void OnPlayerChoseSkill(SkillDefinitionSO skill)
    {
        if (skillHasBeenSelected) return; // Already chose a skill this turn

        Debug.Log($"<color=cyan>PlayerTurnState: Skill '{skill.skillNameKey}' chosen by {activeCharacter.GetName()}.</color>");
        currentSelectedSkill = skill;
        skillHasBeenSelected = true; 

        if (battleUI != null)
        {
            battleUI.HideActionButtons(); // Hide buttons once skill is chosen
        }
    }
    
    public override void Exit()
    {
        Debug.Log("<color=cyan>Player Turn: Exit</color>");
        if (battleUI != null)
        {
            // Buttons should be hidden by OnPlayerChoseSkill or if state exits prematurely
            // battleUI.HideActionButtons(); 
            // Indicator hiding is usually handled by the state that no longer needs it or the one that takes over.
        }
    }
    
    public override void Update()
    {
        if (skillHasBeenSelected)
        {
            if (currentSelectedSkill == null)
            {
                 Debug.LogError("PlayerTurnState: skillHasBeenSelected is true, but currentSelectedSkill is null. This should not happen.");
                 Complete(BattleEvent.ActionFullyComplete, null); // Error case, skip turn
                 return;
            }
            // Pass the chosen SkillDefinitionSO to BattleManager
            Complete(BattleEvent.PlayerSkillSelected, currentSelectedSkill);
        }
        // Handle cancel input if player can back out of skill selection (e.g., to view character stats)
        // This would typically re-show the skill buttons or go to a "command menu" state.
        // For now, once skill is selected, it proceeds.
    }
}