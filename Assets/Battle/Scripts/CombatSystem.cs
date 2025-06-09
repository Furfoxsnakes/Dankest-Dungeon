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
    [SerializeField] private TargetingSystem targetingSystem;
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

        if (skillProcessor == null) // Ensure skillProcessor is assigned or found
            skillProcessor = GetComponent<SkillEffectProcessor>() ?? gameObject.AddComponent<SkillEffectProcessor>();
        
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
        
        if (targetingSystem == null)
            targetingSystem = new TargetingSystem(); // Or GetComponent if it becomes a MonoBehaviour

        if (actionSequenceHandler == null)
            actionSequenceHandler = GetComponent<ActionSequenceHandler>() ?? gameObject.AddComponent<ActionSequenceHandler>();
        
        // Initialize handlers with necessary dependencies
        actionSequenceHandler.Initialize(this, skillProcessor, targetingSystem);
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
    
    public void ExecuteAction(BattleAction action, Action onComplete)
    {
        // Delegate to ActionSequenceHandler
        if (actionSequenceHandler != null)
        {
            actionSequenceHandler.ExecuteAction(action, onComplete);
        }
        else
        {
            Debug.LogError("[COMBAT] ActionSequenceHandler is not initialized!");
            onComplete?.Invoke();
        }
    }
    
    // ... (rest of the CombatSystem class)
}