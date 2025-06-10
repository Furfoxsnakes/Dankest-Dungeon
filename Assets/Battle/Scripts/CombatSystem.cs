using System;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills;
using System.Linq;
using Random = UnityEngine.Random;

public class CombatSystem : MonoBehaviour
{
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
    
    public void Initialize(List<Character> players, List<Character> enemies)
    {
        _playerTeamCharacters = players;
        _enemyTeamCharacters = enemies;
        
        Debug.Log($"[COMBAT] Combat system initialized with {players.Count} players and {enemies.Count} enemies");
        
        foreach (var character in players) { ValidateCharacter(character, "Player"); }
        foreach (var character in enemies) { ValidateCharacter(character, "Enemy"); }
    }

    public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
    {
        _playerTeamCharacters = playerTeam;
        _enemyTeamCharacters = enemyTeam;
        // Pass team data to TargetingSystem
        targetingSystem.InitializeTeams(_playerTeamCharacters, _enemyTeamCharacters);
        // If ActionSequenceHandler needs direct team lists, pass them here too or provide getters.
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
    
    public List<Character> GetPlayerCharacters() => _playerTeamCharacters;
    public List<Character> GetEnemyCharacters() => _enemyTeamCharacters;

    // Public accessors for team lists if needed by other systems or UI
    public List<Character> GetPlayerTeamCharacters() => _playerTeamCharacters;
    public List<Character> GetEnemyTeamCharacters() => _enemyTeamCharacters;

    public TargetingSystem GetTargetingSystem() => targetingSystem; // Add this line

    public SkillEffectProcessor GetSkillEffectProcessor() => skillProcessor; // Add this line

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
    
    // ... (rest of the CombatSystem class)
}