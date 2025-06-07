using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DankestDungeon.Skills; // For SkillDefinitionSO

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

    public UIManager uiManager; // Make sure this is assigned or found correctly if BattleUI is on UIManager
    
    public FormationData GetPlayerFormation() => playerFormation;
    
    public FormationData GetEnemyFormation() => enemyFormation;
    
    public List<Character> GetPlayerCharacters() => playerCharacters ?? new List<Character>();
    
    public List<Character> GetEnemyCharacters() => enemyCharacters ?? new List<Character>();
    
    public BattleUI GetBattleUI()
    {
        if (battleUI != null) return battleUI;
        if (uiManager != null && uiManager.battleUI != null) return uiManager.battleUI;
        
        // Fallback: Try to find BattleUI in the scene directly if not assigned
        // This assumes BattleUI component is on a GameObject named "BattleUI" or similar, or just one instance exists.
        BattleUI foundBattleUI = FindFirstObjectByType<BattleUI>();
        if (foundBattleUI != null) {
            battleUI = foundBattleUI; // Cache it
            return battleUI;
        }

        Debug.LogError("BattleUI could not be found or accessed through BattleManager or UIManager!");
        return null;
    }

    // Method for BattleUI to inform PlayerTurnState about action type selection - REMOVED
    // public void PlayerSelectedActionType(ActionType actionType)
    // {
    //     if (stateMachine.CurrentState is PlayerTurnState playerTurnState)
    //     {
    //         // playerTurnState.PlayerChoseActionType(actionType); // This method would also be removed from PlayerTurnState
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"PlayerSelectedActionType called but current state is not PlayerTurnState. Current state: {stateMachine.CurrentState?.GetType().Name}");
    //     }
    // }
    
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
        Debug.Log($"<color=orange>[BATTLE MANAGER] Handling event: {battleEvent} with data: {data?.GetType().Name ?? "null"}</color>");

        switch (battleEvent)
        {
            case BattleEvent.SetupComplete:
                Debug.Log("<color=orange>[BATTLE MANAGER] Setup complete, changing to Player Turn</color>");
                stateMachine.ChangeState(new PlayerTurnState(this));
                break;
                
            // case BattleEvent.PlayerActionSelected: // This case is now obsolete if all actions are skills
            //     if (data is ActionType actionType)
            //     {
            //         Character currentActor = turnSystem.GetCurrentActor();
            //         Debug.Log($"<color=orange>[BATTLE MANAGER] Player selected action type: {actionType}. Actor: {currentActor.GetName()}</color>");
            //         if (actionType == ActionType.Defend) 
            //         {
            //             BattleAction defendAction = new BattleAction(currentActor, currentActor, ActionType.Defend);
            //             stateMachine.ChangeState(new ActionExecutionState(this, defendAction));
            //         }
            //         // ... other non-skill actions
            //     }
            //     break;

            case BattleEvent.PlayerSkillSelected: 
                if (data is SkillDefinitionSO selectedSkill)
                {
                    Character currentActor = turnSystem.GetCurrentActor();
                    if (currentActor == null || !currentActor.IsAlive)
                    {
                        Debug.LogError($"[BATTLE MANAGER] PlayerSkillSelected: Current actor is null or dead. Skipping.");
                        AdvanceTurn();
                        break;
                    }
                    int skillRank = currentActor.GetSkillRank(selectedSkill);

                    if (skillRank == 0) {
                        Debug.LogError($"[BATTLE MANAGER] Actor {currentActor.GetName()} does not know skill {selectedSkill.skillNameKey} or rank is 0. UI should prevent this.");
                        stateMachine.ChangeState(new PlayerTurnState(this)); 
                        break;
                    }
                    
                    Debug.Log($"<color=orange>[BATTLE MANAGER] Player selected skill: {selectedSkill.skillNameKey} (Rank {skillRank}) by {currentActor.GetName()}.</color>");

                    bool needsTargeting = selectedSkill.targetType == SkillTargetType.SingleEnemy ||
                                          selectedSkill.targetType == SkillTargetType.SingleAlly ||
                                          selectedSkill.targetType == SkillTargetType.EnemyRow || 
                                          selectedSkill.targetType == SkillTargetType.AllyRow;   

                    if (needsTargeting)
                    {
                        BattleAction actionInProgress = new BattleAction(currentActor, selectedSkill, skillRank);
                        stateMachine.ChangeState(new TargetSelectionState(this, actionInProgress));
                    }
                    else 
                    {
                        BattleAction skillAction = new BattleAction(currentActor, null, selectedSkill, skillRank); 
                        stateMachine.ChangeState(new ActionExecutionState(this, skillAction));
                    }
                }
                else
                {
                    Debug.LogError("[BATTLE MANAGER] PlayerSkillSelected event did not provide a SkillDefinitionSO.");
                    stateMachine.ChangeState(new PlayerTurnState(this)); 
                }
                break;

            case BattleEvent.TargetSelected: 
                if (data is BattleAction actionWithTarget)
                {
                    Debug.Log($"<color=orange>[BATTLE MANAGER] Target selected. Action: {actionWithTarget.ActionType} by {actionWithTarget.Actor.GetName()} on {actionWithTarget.Target?.GetName()}. Skill: {actionWithTarget.UsedSkill?.skillNameKey}. Transitioning to ActionExecutionState.</color>");
                    stateMachine.ChangeState(new ActionExecutionState(this, actionWithTarget));
                }
                else
                {
                    Debug.LogError("[BATTLE MANAGER] TargetSelected event did not provide a BattleAction.");
                    stateMachine.ChangeState(new PlayerTurnState(this));
                }
                break;

            case BattleEvent.TargetSelectionCancelled: 
                Debug.Log("<color=orange>[BATTLE MANAGER] Target selection cancelled. Returning to Player Turn.</color>");
                stateMachine.ChangeState(new PlayerTurnState(this));
                break;

            case BattleEvent.EnemyActionComplete: 
                if (data is BattleAction enemyAction)
                {
                    Debug.Log($"<color=orange>[BATTLE MANAGER] Enemy action decided: {enemyAction.ActionType} by {enemyAction.Actor.GetName()} on {enemyAction.Target?.GetName()}. Transitioning to ActionExecutionState.</color>");
                    stateMachine.ChangeState(new ActionExecutionState(this, enemyAction));
                }
                else
                {
                    Debug.LogError("[BATTLE MANAGER] EnemyActionComplete event did not provide a BattleAction. Advancing turn to prevent stall.");
                    AdvanceTurn(); 
                }
                break;

            case BattleEvent.ActionFullyComplete: 
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
        return enemyCharacters.All(e => !e.IsAlive);
    }
    
    private bool AllPlayersDefeated()
    {
        return playerCharacters.All(p => !p.IsAlive);
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

        // Initialize the CombatSystem with the team lists
        if (combatSystem != null)
        {
            combatSystem.InitializeTeams(playerCharacters, enemyCharacters);
            Debug.Log("[BATTLE MANAGER] Character lists set and CombatSystem initialized with teams.");
        }
        else
        {
            Debug.LogError("[BATTLE MANAGER] CombatSystem is null. Cannot initialize teams for contextual targeting.");
        }
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