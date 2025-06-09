using UnityEngine;

public class EnemyTurnState : BattleState
{
    private float aiThinkingTime = 1.0f;
    private float timer = 0f;
    private bool actionPerformed = false;
    private BattleAction decidedAction;
    private SkillEffectProcessor skillEffectProcessor; // Added
    private Character currentEnemy; // Added to store the current actor

    public EnemyTurnState(BattleManager manager) : base(manager)
    {
        skillEffectProcessor = manager.GetSkillEffectProcessor(); // Get from BattleManager
        if (skillEffectProcessor == null) Debug.LogError("SkillEffectProcessor not found for EnemyTurnState via BattleManager.");
    }

    public override void Enter()
    {
        Debug.Log("<color=red>Enemy Turn: Enter</color>");
        timer = 0f;
        actionPerformed = false;
        decidedAction = null;
        currentEnemy = battleManager.GetTurnSystem().GetCurrentActor();

        if (currentEnemy == null || !currentEnemy.IsAlive)
        {
            Debug.LogWarning($"EnemyTurnState: Active enemy {currentEnemy?.GetName()} is null or not alive. Advancing turn.");
            Complete(BattleEvent.EnemyActionComplete, new BattleAction(currentEnemy, currentEnemy, ActionType.Skip)); // Skip turn
            return;
        }

        // Tick status effects at the start of the enemy's turn
        if (skillEffectProcessor != null)
        {
            Debug.Log($"<color=yellow>[EnemyTurnState] Ticking status effects for {currentEnemy.GetName()}</color>");
            currentEnemy.TickStatusEffects(skillEffectProcessor);
        }
        else
        {
            Debug.LogError("SkillEffectProcessor is null in EnemyTurnState. Cannot tick status effects.");
        }

        // Check if enemy died from status effects
        if (!currentEnemy.IsAlive)
        {
            Debug.LogWarning($"EnemyTurnState: Active enemy {currentEnemy.GetName()} died from status effects. Advancing turn.");
            // Ensure decidedAction is at least a Skip action if the enemy died before deciding.
            if (decidedAction == null)
            {
                decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
            }
            Complete(BattleEvent.EnemyActionComplete, decidedAction); // Skip turn
            return;
        }

        // Get the enemy's action decision
        PerformAIDecision();
    }

    public override void Update()
    {
        // If the enemy died during status ticks and already completed, do nothing.
        if (currentEnemy == null || !currentEnemy.IsAlive && actionPerformed) return;
        
        if (!actionPerformed)
        {
            timer += Time.deltaTime;

            if (timer >= aiThinkingTime)
            {
                actionPerformed = true;

                // Ensure decidedAction is not null (e.g., if PerformAIDecision had an issue but enemy is still alive)
                if (decidedAction == null)
                {
                    Debug.LogWarning($"EnemyTurnState: decidedAction was null for {currentEnemy.GetName()} after thinking time. Forcing Skip.");
                    decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
                }
                
                // Send the pre-decided action to the battle manager
                Complete(BattleEvent.EnemyActionComplete, decidedAction);
            }
        }
    }

    private void PerformAIDecision()
    {
        // currentEnemy is already set in Enter() and checked for null/alive
        if (currentEnemy == null || !currentEnemy.IsAlive)
        {
            // This case should ideally be caught earlier, but as a safeguard:
            Debug.LogWarning($"PerformAIDecision called with a null or dead enemy: {currentEnemy?.GetName()}. Defaulting to Skip.");
            decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
            return;
        }
        
        // Ensure it's actually an Enemy type
        if (currentEnemy is Enemy enemyAI) // Renamed 'enemy' to 'enemyAI' to avoid conflict
        {
            // Get the AI decision from the enemy
            decidedAction = enemyAI.DecideAction(
                battleManager.GetEnemyCharacters(),
                battleManager.GetPlayerCharacters()
            );

            if (decidedAction != null && decidedAction.Target != null)
            {
                Debug.Log($"[ENEMY AI] {currentEnemy.GetName()} decided to use '{decidedAction.ActionType}' (Skill: {decidedAction.UsedSkill?.skillNameKey}) on {decidedAction.Target.GetName()}");
            }
            else if (decidedAction != null)
            {
                 Debug.Log($"[ENEMY AI] {currentEnemy.GetName()} decided to use '{decidedAction.ActionType}' (Skill: {decidedAction.UsedSkill?.skillNameKey}) with no specific target (e.g. self-cast or AoE).");
            }
            else
            {
                Debug.LogWarning($"[ENEMY AI] {currentEnemy.GetName()} failed to decide an action. Defaulting to Skip.");
                decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
            }
        }
        else
        {
            // Fallback for non-enemy characters (e.g. if a player character was somehow put in enemy team and got a turn)
            Debug.LogWarning($"Current actor '{currentEnemy.GetName()}' is not an Enemy type! Defaulting to Skip.");
            decidedAction = new BattleAction(
                currentEnemy,
                currentEnemy,
                ActionType.Skip
            );
        }
    }
}