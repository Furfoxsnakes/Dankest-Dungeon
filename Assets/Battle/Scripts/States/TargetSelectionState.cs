using UnityEngine;
using DankestDungeon.Skills; // For SkillDefinitionSO and SkillTargetType
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

public class TargetSelectionState : BattleState
{
    private Character _actingCharacter;
    private SkillDefinitionSO _skillToUse; // Store the skill being targeted
    private SkillRankData _skillRankToUse; // Store the specific rank of the skill
    private List<Character> _validTargets;
    private int _currentTargetIndex = -1;
    private BattleUI battleUI; // Declare battleUI field

    // Modified constructor to accept SkillRankData
    public TargetSelectionState(BattleManager manager, Character actingCharacter, SkillDefinitionSO skill, SkillRankData skillRank) : base(manager)
    {
        _actingCharacter = actingCharacter;
        _skillToUse = skill;
        _skillRankToUse = skillRank; // Store the passed SkillRankData
        this.battleUI = manager.GetBattleUI(); // Initialize battleUI from BattleManager

        if (_actingCharacter == null || _skillToUse == null || _skillRankToUse == null)
        {
            Debug.LogError($"TargetSelectionState initialized with null actor, skill, or skillRank. Actor: {_actingCharacter?.GetName()}, Skill: {_skillToUse?.skillNameKey}, Rank: {(_skillRankToUse != null ? _skillRankToUse.rankLevel.ToString() : "NULL")}");
        }
        if (this.battleUI == null)
        {
            Debug.LogError("TargetSelectionState could not get BattleUI from BattleManager.");
        }
    }

    public override void Enter()
    {
        Debug.Log($"<color=green>Target Selection: Enter</color> - {_actingCharacter.GetName()} using {_skillToUse.skillNameKey} (Rank: {_skillRankToUse.rankLevel})");
        
        TargetingSystem targetingSystem = battleManager.GetTargetingSystem();
        if (targetingSystem != null)
        {
            _validTargets = targetingSystem.GetValidTargets(_actingCharacter, _skillToUse);
        }
        else
        {
            Debug.LogError("TargetingSystem is null in TargetSelectionState. Cannot get valid targets.");
            _validTargets = new List<Character>(); // Initialize to empty list to prevent null reference later
        }

        if (_validTargets == null || _validTargets.Count == 0)
        {
            Debug.LogWarning($"No valid targets for skill {_skillToUse.skillNameKey}. Cancelling action.");
            // Use the stored _skillRankToUse
            Complete(BattleEvent.TargetSelectionCancelled, new BattleAction(_actingCharacter, null, _skillToUse, _skillRankToUse));
            return;
        }

        _currentTargetIndex = 0;
        UpdateTargetIndicator(); // This will now use the initialized battleUI

        InputManager.UINavigatePerformed += HandleTargetSwitchEvent;
        InputManager.UIConfirmPerformed += HandleTargetConfirmEvent;
        InputManager.UICancelPerformed += HandleCancelEvent;
    }

    private void HandleTargetSwitchEvent(Vector2 direction)
    {
        if (_validTargets == null || _validTargets.Count <= 1) return; // Added null check for _validTargets

        int change = 0;
        if (Mathf.Abs(direction.x) > 0.5f)
        {
            change = (int)Mathf.Sign(direction.x);
        }
        else if (Mathf.Abs(direction.y) > 0.5f)
        {
            change = -(int)Mathf.Sign(direction.y);
        }

        if (change != 0)
        {
            _currentTargetIndex = (_currentTargetIndex + change + _validTargets.Count) % _validTargets.Count;
            UpdateTargetIndicator();
        }
    }

    private void HandleTargetConfirmEvent() // This method is called by InputManager
    {
        if (_validTargets == null) 
        {
            Debug.LogError("HandleTargetConfirmEvent: _validTargets is null!");
            return;
        }
        if (_currentTargetIndex >= 0 && _currentTargetIndex < _validTargets.Count)
        {
            Character confirmedTarget = _validTargets[_currentTargetIndex];
            // Use the stored _skillRankToUse
            BattleAction finalAction = new BattleAction(_actingCharacter, confirmedTarget, _skillToUse, _skillRankToUse);
            Debug.Log($"<color=yellow>Target Confirmed: {confirmedTarget.GetName()} for skill {_skillToUse.skillNameKey} (Rank: {_skillRankToUse.rankLevel})</color>");
            
            if (battleUI != null) battleUI.HideTargetIndicator(); 
            Complete(BattleEvent.TargetSelected, finalAction);
        }
        else
        {
            Debug.LogWarning("TargetSelectionState: Confirm (HandleTargetConfirmEvent) pressed with no valid target selected.");
        }
    }

    private void HandleCancelEvent() // This method is called by InputManager
    {
        Debug.Log("<color=yellow>Target Selection Cancelled by player (HandleCancelEvent).</color>");
        if (battleUI != null) battleUI.HideTargetIndicator(); 
        // Use the stored _skillRankToUse
        BattleAction cancelledAction = new BattleAction(_actingCharacter, null, _skillToUse, _skillRankToUse);
        Complete(BattleEvent.TargetSelectionCancelled, cancelledAction);
    }

    private void UpdateTargetIndicator()
    {
        if (_validTargets == null) // Added null check
        {
            if (battleUI != null) battleUI.HideTargetIndicator();
            return;
        }
        if (battleUI != null && _currentTargetIndex >= 0 && _currentTargetIndex < _validTargets.Count)
        {
            battleUI.ShowTargetIndicator(_validTargets[_currentTargetIndex]);
        }
        else if (battleUI != null) // If no valid target or index is out of bounds, hide indicator
        {
            battleUI.HideTargetIndicator();
        }
    }

    public override void Exit()
    {
        Debug.Log("<color=yellow>Target Selection: Exit</color>");
        InputManager.UINavigatePerformed -= HandleTargetSwitchEvent;
        InputManager.UIConfirmPerformed -= HandleTargetConfirmEvent;
        InputManager.UICancelPerformed -= HandleCancelEvent;

        if (battleUI != null) // battleUI is now accessible
        {
            battleUI.HideTargetIndicator();
        }
    }

    // The following OnConfirm and OnCancel methods are likely deprecated if you're using static events from InputManager.
    // If they are still needed for some other input path, they would also need the 'using UnityEngine.InputSystem;'
    // For now, I'm assuming HandleTargetConfirmEvent and HandleCancelEvent are the primary handlers.
    // If these are truly unused, you can remove them.

    // protected override void OnConfirm(InputAction.CallbackContext context)
    // {
    //     if (_currentTargetIndex >= 0 && _currentTargetIndex < _validTargets.Count)
    //     {
    //         Character selectedTarget = _validTargets[_currentTargetIndex];
    //         Debug.Log($"Target selected: {selectedTarget.GetName()} for skill {_skillToUse.skillNameKey}");
    //         BattleAction finalAction = new BattleAction(_actingCharacter, selectedTarget, _skillToUse);
    //         Complete(BattleEvent.TargetSelected, finalAction);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("TargetSelectionState: Confirm (OnConfirm) pressed with no valid target selected.");
    //     }
    // }

    // protected override void OnCancel(InputAction.CallbackContext context)
    // {
    //     Debug.Log("Target selection cancelled (OnCancel).");
    //     BattleAction cancelledAction = new BattleAction(_actingCharacter, null, _skillToUse);
    //     Complete(BattleEvent.TargetSelectionCancelled, cancelledAction);
    // }
}