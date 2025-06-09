using UnityEngine;
using System.Collections.Generic;

public class StartState : BattleState
{
    private bool setupComplete = false;
    private float setupTimer = 0f;
    private const float INTRO_DURATION = 1.5f; // Time for intro animations/effects
    
    public StartState(BattleManager manager) : base(manager) { }
    
    public override void Enter()
    {
        Debug.Log("Entering Start state - Battle initialization beginning");
        setupTimer = 0f;
        setupComplete = false;
        
        // Initialize core battle systems
        InitializeBattleSystems();
        
        // Play intro animations/effects
        PlayBattleIntro();
    }
    
    public override void Update()
    {
        if (!setupComplete)
        {
            setupTimer += Time.deltaTime;
            
            // Once intro animations are done, complete the state
            if (setupTimer >= INTRO_DURATION)
            {
                setupComplete = true;
                Debug.Log("Battle setup complete, transitioning to first turn");
                Complete(BattleEvent.SetupComplete);
            }
        }
    }
    
    private void InitializeBattleSystems()
    {
        // Spawn characters in formations first
        SpawnFormations();
        
        // Then initialize systems with the spawned characters
        var turnSystem = battleManager.GetTurnSystem();
        turnSystem.Initialize(battleManager.GetPlayerCharacters(), battleManager.GetEnemyCharacters());
        
        var combatSystem = battleManager.GetCombatSystem();
        // Updated method name here
        combatSystem.InitializeTeams(battleManager.GetPlayerCharacters(), battleManager.GetEnemyCharacters());
        
        // Set up UI elements for battle
        SetupBattleUI();
    }
    
    private void SpawnFormations()
    {
        FormationManager formationManager = battleManager.GetFormationManager();
        
        // Use direct FormationData references
        List<Character> playerCharacters = formationManager.SpawnFormation(
            battleManager.GetPlayerFormation());
        
        List<Character> enemyCharacters = formationManager.SpawnFormation(
            battleManager.GetEnemyFormation());
        
        // Update BattleManager with spawned characters
        battleManager.SetCharacterLists(playerCharacters, enemyCharacters);
        
        Debug.Log($"Battle formations created: {playerCharacters.Count} heroes vs {enemyCharacters.Count} enemies");
    }
    
    private void SetupBattleUI()
    {
        // Initialize battle UI elements
        Debug.Log("Setting up battle UI");
        
        // In a real implementation, you would:
        // - Show character portraits
        // - Initialize health bars
        // - Display battle controls
        // - Set up turn order display
    }
    
    private void PlayBattleIntro()
    {
        // Play battle intro animations/effects
        Debug.Log("Playing battle intro animation");
        
        // In a real implementation, you would:
        // - Play battle start audio
        // - Show "Battle Start" text animation
        // - Zoom camera to battle view
        // - Fade in UI elements
    }
}