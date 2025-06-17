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
    [SerializeField] private FormationManager formationManager; // <<< ADD THIS LINE, assign in Inspector

    private List<Character> _playerTeamCharacters;
    private List<Character> _enemyTeamCharacters;

    private void Awake()
    {
        if (battleUI == null)
        {
            Debug.LogError("[CombatSystem] BattleUI is not assigned in the Inspector!");
        }

        if (formationManager == null) // <<< ADD THIS CHECK
        {
            Debug.LogError("[CombatSystem] FormationManager is not assigned in the Inspector!");
        }

        if (skillProcessor == null) 
            skillProcessor = GetComponent<SkillEffectProcessor>() ?? gameObject.AddComponent<SkillEffectProcessor>();
        
        if (skillProcessor != null)
        {
            if (battleUI != null && formationManager != null) // <<< UPDATE CONDITION
            {
                // Pass both BattleUI and FormationManager
                skillProcessor.Initialize(battleUI, formationManager); // <<< MODIFIED CALL
            }
            else
            {
                Debug.LogError("[CombatSystem] Cannot initialize SkillEffectProcessor because BattleUI or FormationManager is missing.");
            }
        }
        else
        {
            Debug.LogError("[CombatSystem] SkillProcessor is not assigned or found.");
        }
        
        if (targetingSystem == null)
            targetingSystem = new TargetingSystem(); 

        if (actionSequenceHandler == null)
            actionSequenceHandler = GetComponent<ActionSequenceHandler>() ?? gameObject.AddComponent<ActionSequenceHandler>();
        
        actionSequenceHandler.Initialize(this, skillProcessor, targetingSystem);
    }
    
    public void Initialize(List<Character> players, List<Character> enemies)
    {
        _playerTeamCharacters = players;
        _enemyTeamCharacters = enemies;
        
        Debug.Log($"[COMBAT] Combat system initialized with {players.Count} players and {enemies.Count} enemies");
        
        foreach (var character in players) { ValidateCharacter(character, "Player"); }
        foreach (var character in enemies) { ValidateCharacter(character, "Enemy"); }

        // Initialize TargetingSystem with teams here if not done elsewhere or if it needs re-initialization
        if (targetingSystem != null)
        {
            targetingSystem.InitializeTeams(_playerTeamCharacters, _enemyTeamCharacters);
        }
    }

    // This InitializeTeams method seems redundant if the above Initialize does the same.
    // If it's called separately, ensure targetingSystem is also updated.
    public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
    {
        _playerTeamCharacters = playerTeam;
        _enemyTeamCharacters = enemyTeam;
        if (targetingSystem != null)
        {
            targetingSystem.InitializeTeams(_playerTeamCharacters, _enemyTeamCharacters);
        }
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