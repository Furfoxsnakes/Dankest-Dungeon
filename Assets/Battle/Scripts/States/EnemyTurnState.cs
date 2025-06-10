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
            // For Skip, PrimaryTarget can be self. ResolvedTargets can be a list containing self or null.
            Complete(BattleEvent.EnemyActionComplete, new BattleAction(currentEnemy, currentEnemy, ActionType.Skip)); 
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
            if (decidedAction == null)
            {
                // For Skip, PrimaryTarget can be self.
                decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
            }
            Complete(BattleEvent.EnemyActionComplete, decidedAction); 
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

                if (decidedAction == null)
                {
                    Debug.LogWarning($"EnemyTurnState: decidedAction was null for {currentEnemy.GetName()} after thinking time. Forcing Skip.");
                    // For Skip, PrimaryTarget can be self.
                    decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
                }
                
                Complete(BattleEvent.EnemyActionComplete, decidedAction);
            }
        }
    }

    private void PerformAIDecision()
    {
        if (currentEnemy == null || !currentEnemy.IsAlive)
        {
            Debug.LogWarning($"PerformAIDecision called with a null or dead enemy: {currentEnemy?.GetName()}. Defaulting to Skip.");
            // For Skip, PrimaryTarget can be self.
            decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
            return;
        }
        
        if (currentEnemy is Enemy enemyAI) 
        {
            decidedAction = enemyAI.DecideAction(
                battleManager.GetEnemyCharacters(),
                battleManager.GetPlayerCharacters()
            );

            // The Enemy.DecideAction method is now responsible for setting PrimaryTarget 
            // and ResolvedTargets within the BattleAction it returns.
            if (decidedAction != null && decidedAction.PrimaryTarget != null)
            {
                Debug.Log($"[ENEMY AI] {currentEnemy.GetName()} decided to use '{decidedAction.ActionType}' (Skill: {decidedAction.UsedSkill?.skillNameKey}) on {decidedAction.PrimaryTarget.GetName()}. Resolved targets: {decidedAction.ResolvedTargets?.Count ?? 0}");
            }
            else if (decidedAction != null)
            {
                 Debug.Log($"[ENEMY AI] {currentEnemy.GetName()} decided to use '{decidedAction.ActionType}' (Skill: {decidedAction.UsedSkill?.skillNameKey}) with no specific primary target (e.g. self-cast or broad AoE). Resolved targets: {decidedAction.ResolvedTargets?.Count ?? 0}");
            }
            else
            {
                Debug.LogWarning($"[ENEMY AI] {currentEnemy.GetName()} failed to decide an action. Defaulting to Skip.");
                // For Skip, PrimaryTarget can be self.
                decidedAction = new BattleAction(currentEnemy, currentEnemy, ActionType.Skip);
            }
        }
        else
        {
            Debug.LogWarning($"Current actor '{currentEnemy.GetName()}' is not an Enemy type! Defaulting to Skip.");
            // For Skip, PrimaryTarget can be self.
            decidedAction = new BattleAction(
                currentEnemy,
                currentEnemy,
                ActionType.Skip
            );
        }
    }
}