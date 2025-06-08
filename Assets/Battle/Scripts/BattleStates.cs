using UnityEngine;

/// <summary>
/// Base class for all battle states
/// </summary>
public abstract class BattleState : IState // No longer needs IResettableCompletion separately
{
    protected BattleManager battleManager;
    private bool isComplete = false;
    private BattleEvent resultEvent = BattleEvent.None; // Assuming BattleEvent is your specific event type
    private object resultData = null;

    public bool IsComplete => isComplete;
    public object Result => resultEvent; // Similar to CharacterState, this returns the specific event type
    public object ResultData => resultData; // Specific to BattleState, not part of IState

    public BattleState(BattleManager manager)
    {
        battleManager = manager;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }

    // Original method for simple completion
    protected void Complete(BattleEvent result)
    {
        resultEvent = result;
        isComplete = true;
        resultData = null;
        // Debug.Log($"[BattleState] {this.GetType().Name} completed with {result}");
    }

    // New overload for completion with additional data
    protected void Complete(BattleEvent result, object data)
    {
        resultEvent = result;
        isComplete = true;
        resultData = data;
        // Debug.Log($"[BattleState] {this.GetType().Name} completed with {result} and data: {data}");
    }
    
    // Implementation of ResetCompletionStatus from IState
    public virtual void ResetCompletionStatus()
    {
        isComplete = false;
        resultEvent = BattleEvent.None; // Reset to a default event
        resultData = null; // Reset associated data
        // Debug.Log($"[BattleState] {this.GetType().Name} completion status reset.");
    }
}