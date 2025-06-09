using System;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills;
using System.Linq;
using Random = UnityEngine.Random;

public class CombatSystem : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private SkillEffectProcessor skillProcessor;
    [SerializeField] private ActionSequenceHandler actionSequenceHandler;
    [SerializeField] private TargetingSystem targetingSystem; // This should be assigned
    [SerializeField] private BattleUI battleUI; // Assign in Inspector

    private List<Character> _playerTeamCharacters;
    private List<Character> _enemyTeamCharacters;

    private void Awake()
    {
        if (battleUI == null)
        {
            Debug.LogError("[CombatSystem] BattleUI is not assigned in the Inspector!");
            // Optionally, try to find it as a fallback, but Inspector assignment is preferred
            // battleUI = FindFirstObjectByType<BattleUI>();
        }

        if (actionSequenceHandler == null)
            actionSequenceHandler = GetComponent<ActionSequenceHandler>() ?? gameObject.AddComponent<ActionSequenceHandler>();
        
        if (skillProcessor == null)
            skillProcessor = GetComponent<SkillEffectProcessor>() ?? gameObject.AddComponent<SkillEffectProcessor>();
        
        if (targetingSystem == null) // Ensure TargetingSystem is either a MonoBehaviour on the same GameObject or assigned
            targetingSystem = GetComponent<TargetingSystem>(); // Or however you get its reference

        // Crucial call:
        if (actionSequenceHandler != null && skillProcessor != null && targetingSystem != null)
        {
            actionSequenceHandler.Initialize(this, skillProcessor, targetingSystem);
        }
        else
        {
            Debug.LogError("[CombatSystem] Failed to initialize ActionSequenceHandler due to missing dependencies (ASH, SP, or TS).");
            if(actionSequenceHandler == null) Debug.LogError("[CombatSystem] actionSequenceHandler is null.");
            if(skillProcessor == null) Debug.LogError("[CombatSystem] skillProcessor is null.");
            if(targetingSystem == null) Debug.LogError("[CombatSystem] targetingSystem is null.");
        }
        
        // Initialize SkillEffectProcessor with BattleUI
        if (skillProcessor != null)
        {
            if (battleUI != null)
            {
                skillProcessor.Initialize(battleUI);
            }
            else
            {
                Debug.LogError("[CombatSystem] Cannot initialize SkillEffectProcessor because BattleUI is missing.");
            }
        }
        else
        {
            Debug.LogError("[CombatSystem] SkillProcessor is not assigned or found.");
        }
    }
    
    public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
    {
        _playerTeamCharacters = playerTeam;
        _enemyTeamCharacters = enemyTeam;
        // Pass team data to TargetingSystem
        if (targetingSystem != null)
        {
            targetingSystem.InitializeTeams(_playerTeamCharacters, _enemyTeamCharacters);
        }
        else
        {
            Debug.LogError("[CombatSystem] TargetingSystem is null, cannot initialize teams.");
        }
        
        Debug.Log($"[COMBAT] Combat system initialized with {playerTeam.Count} players and {enemyTeam.Count} enemies");
        
        foreach (var character in playerTeam) { ValidateCharacter(character, "Player"); }
        foreach (var character in enemyTeam) { ValidateCharacter(character, "Enemy"); }
    }
    
    private void ValidateCharacter(Character character, string type)
    {
        if (character == null) { Debug.LogError($"[COMBAT] Null {type} character found during initialization!"); return; }
        if (!character.IsAlive)
        {
            Debug.LogWarning($"[COMBAT] {type} character {character.GetName()} is not alive at battle start!");
        }
        
        if (character.characterAnimator == null)
        {
            Debug.LogWarning($"[COMBAT] {type} character {character.GetName()} has no animator!");
        }
        
        Debug.Log($"[COMBAT] {type} character {character.GetName()} validated for combat");
    }
    
    // Public accessors for team lists if needed by other systems or UI
    public List<Character> GetPlayerTeamCharacters() => _playerTeamCharacters;
    public List<Character> GetEnemyTeamCharacters() => _enemyTeamCharacters;
    
    public void ExecuteAction(BattleAction action, System.Action onComplete)
    {
        if (actionSequenceHandler != null)
        {
            actionSequenceHandler.ProcessAction(action, onComplete); // Renamed ExecuteAction to ProcessAction
        }
        else
        {
            Debug.LogError("[COMBAT] ActionSequenceHandler is not initialized!");
            onComplete?.Invoke();
        }
    }

    public SkillEffectProcessor GetSkillEffectProcessor() => skillProcessor; // Add this getter

    public TargetingSystem GetTargetingSystem() // Add this getter
    {
        return targetingSystem;
    }
}