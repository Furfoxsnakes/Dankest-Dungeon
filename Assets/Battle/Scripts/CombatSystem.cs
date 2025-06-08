using System;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills;
using System.Linq;
using Random = UnityEngine.Random;

public class CombatSystem : MonoBehaviour
{
    [SerializeField] private SkillEffectProcessor skillProcessor;
    [SerializeField] private ActionSequenceHandler actionSequenceHandler; // New
    [SerializeField] private TargetingSystem targetingSystem; // New
    
    private List<Character> playerCharacters;
    private List<Character> enemyCharacters;
    
    private List<Character> _playerTeamCharacters;
    private List<Character> _enemyTeamCharacters;

    private void Awake()
    {
        if (skillProcessor == null)
            skillProcessor = GetComponent<SkillEffectProcessor>() ?? gameObject.AddComponent<SkillEffectProcessor>();
        
        if (targetingSystem == null)
            targetingSystem = new TargetingSystem(); // Or GetComponent if it becomes a MonoBehaviour

        if (actionSequenceHandler == null)
            actionSequenceHandler = GetComponent<ActionSequenceHandler>() ?? gameObject.AddComponent<ActionSequenceHandler>();
        
        // Initialize handlers with necessary dependencies
        actionSequenceHandler.Initialize(this, skillProcessor, targetingSystem);
    }
    
    public void Initialize(List<Character> players, List<Character> enemies)
    {
        playerCharacters = players;
        enemyCharacters = enemies;
        
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
    
    public List<Character> GetPlayerCharacters() => playerCharacters;
    public List<Character> GetEnemyCharacters() => enemyCharacters;

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
    
    // ... All Execute...Sequence methods, ApplySkillEffectToTargetWithSequence,
    // ... CalculateDamage, CalculateMagicDamage, and DetermineFinalTargets
    // ... would be REMOVED from this file and moved to their respective new classes.
}