using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private CombatSystem combatSystem;
    [SerializeField] private TurnSystem turnSystem;
    [SerializeField] private FormationManager formationManager;
    [SerializeField] private BattleUI battleUI; // Added BattleUI reference
    
    [Header("Formation Settings")]
    [SerializeField] private FormationData playerFormation; // Direct reference instead of string
    [SerializeField] private FormationData enemyFormation;  // Direct reference instead of string
    
    private List<Character> playerCharacters;
    private List<Character> enemyCharacters;
    
    private StateMachine<BattleState> stateMachine;
    
    void Awake()
    {
        // Initialize systems
        if (combatSystem == null)
            combatSystem = GetComponent<CombatSystem>() ?? gameObject.AddComponent<CombatSystem>();
            
        if (turnSystem == null)
            turnSystem = GetComponent<TurnSystem>() ?? gameObject.AddComponent<TurnSystem>();
            
        if (formationManager == null)
            formationManager = GetComponent<FormationManager>() ?? gameObject.AddComponent<FormationManager>();
            
        // Initialize the state machine
        stateMachine = new StateMachine<BattleState>();
    }
    
    // Accessor methods for states to use
    public TurnSystem GetTurnSystem() => turnSystem;
    
    public CombatSystem GetCombatSystem() => combatSystem;
    
    public FormationManager GetFormationManager() => formationManager;
    
    public FormationData GetPlayerFormation() => playerFormation;
    
    public FormationData GetEnemyFormation() => enemyFormation;
    
    public List<Character> GetPlayerCharacters() => playerCharacters ?? new List<Character>();
    
    public List<Character> GetEnemyCharacters() => enemyCharacters ?? new List<Character>();
    
    public BattleUI GetBattleUI() => battleUI; // Getter for BattleUI
    
    void Start()
    {
        InitializeBattle();
    }
    
    void Update()
    {
        // Update the current state
        stateMachine.Update();
        
        // Use the more detailed debug version instead
        CheckStateCompletion();
    }
    
    private void InitializeBattle()
    {
        // We no longer need to initialize systems here, as StartState will handle it
        // Just initialize the state machine with the StartState
        stateMachine.Initialize(new StartState(this));
    }
    
    private void HandleStateResult(BattleEvent battleEvent, object data = null)
    {
        Debug.Log($"<color=orange>[BATTLE MANAGER] Handling event: {battleEvent}</color>");

        switch (battleEvent)
        {
            case BattleEvent.SetupComplete:
                Debug.Log("<color=orange>[BATTLE MANAGER] Setup complete, changing to Player Turn</color>");
                stateMachine.ChangeState(new PlayerTurnState(this));
                break;
                
            case BattleEvent.PlayerActionSelected:
                Debug.Log("<color=orange>[BATTLE MANAGER] Player action selected, preparing to execute action.</color>");
                // Get the action details from PlayerTurnState or a shared context if needed
                // For now, let's assume ExecuteSelectedAction can determine the action.
                // We need to pass the determined action to ActionExecutionState.
                
                // Determine the action first
                Character attacker = turnSystem.GetCurrentActor();
                var aliveEnemies = enemyCharacters.FindAll(e => e.IsAlive());
                if (aliveEnemies.Count == 0) {
                    Debug.LogWarning("[BATTLE MANAGER] No alive enemies to target for player action.");
                    AdvanceTurn(); // Or handle appropriately
                    return;
                }
                Character target = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
                BattleAction actionToExecute = new BattleAction(attacker, target, ActionType.Attack); // Example

                stateMachine.ChangeState(new ActionExecutionState(this, actionToExecute)); // Pass the action
                break;
                
            case BattleEvent.EnemyActionComplete: // This case might also need to go through ActionExecutionState
                Debug.Log("<color=orange>[BATTLE MANAGER] Enemy action selected, preparing to execute action.</color>");
                if (data is BattleAction enemyAction)
                {
                    stateMachine.ChangeState(new ActionExecutionState(this, enemyAction));
                }
                else
                {
                    Debug.LogError("[BATTLE MANAGER] EnemyActionComplete event did not provide a BattleAction.");
                    AdvanceTurn();
                }
                break;
                
            case BattleEvent.ActionFullyComplete: // This event is now signaled by ActionExecutionState
                Debug.Log("<color=orange>[BATTLE MANAGER] ★★★ Action fully completed (from ActionExecutionState), checking battle state ★★★</color>");
                CheckBattleState();
                break;
            
            case BattleEvent.VictoryProcessed:
                Debug.Log("<color=orange>[BATTLE MANAGER] Victory processed, returning to map</color>");
                ReturnToMap();
                break;
                
            case BattleEvent.DefeatProcessed:
                Debug.Log("<color=orange>[BATTLE MANAGER] Defeat processed, returning to town</color>");
                ReturnToTown();
                break;
        }
    }

    private void CheckBattleState()
    {
        if (AllEnemiesDefeated())
            stateMachine.ChangeState(new VictoryState(this));
        else if (AllPlayersDefeated())
            stateMachine.ChangeState(new DefeatState(this));
        else
            AdvanceTurn();
    }
    
    private void AdvanceTurn()
    {
        Character nextActor = turnSystem.GetNextActor();
        
        if (playerCharacters.Contains(nextActor))
            stateMachine.ChangeState(new PlayerTurnState(this));
        else
            stateMachine.ChangeState(new EnemyTurnState(this));
    }
    
    private bool AllEnemiesDefeated()
    {
        return enemyCharacters.All(e => !e.IsAlive());
    }
    
    private bool AllPlayersDefeated()
    {
        return playerCharacters.All(p => !p.IsAlive());
    }
    
    private void ReturnToMap()
    {
        // Implementation to return to world map
        Debug.Log("Returning to map...");
    }
    
    private void ReturnToTown()
    {
        // Implementation to return to town
        Debug.Log("Returning to town...");
    }
    
    // Add method to set character lists (called from StartState)
    public void SetCharacterLists(List<Character> players, List<Character> enemies)
    {
        playerCharacters = players;
        enemyCharacters = enemies;
    }

    private void OnValidate()
    {
        // Validation to prevent incorrect formation assignments
        if (playerFormation != null && playerFormation.formationType != FormationType.PlayerParty)
        {
            Debug.LogWarning($"Formation '{playerFormation.name}' is not a player formation!");
        }
        
        if (enemyFormation != null && enemyFormation.formationType != FormationType.EnemyGroup)
        {
            Debug.LogWarning($"Formation '{enemyFormation.name}' is not an enemy formation!");
        }
    }

    // Check if current state is complete and respond to its events
    private void CheckStateCompletion()
    {
        if (stateMachine.CheckStateCompletion(out object result))
        {
            BattleState completedState = stateMachine.CurrentState;
            Debug.Log($"<color=magenta>[BATTLE MANAGER] State completed: {completedState.GetType().Name} with result: {result}</color>");
            
            if (result is BattleEvent battleEvent)
            {
                object resultData = completedState.ResultData;
                if (resultData != null)
                {
                    Debug.Log($"<color=magenta>[BATTLE MANAGER] State result data: {resultData.GetType().Name}</color>");
                }
                
                HandleStateResult(battleEvent, resultData);
            }
            else
            {
                Debug.LogWarning($"<color=red>[BATTLE MANAGER] State completed with non-BattleEvent result: {result}</color>");
            }
        }
    }
}