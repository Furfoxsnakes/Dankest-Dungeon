using UnityEngine;

/// <summary>
/// Base class for all battle states
/// </summary>
public abstract class BattleState : IState
{
    protected BattleManager battleManager;
    private bool isComplete = false;
    private BattleEvent resultEvent = BattleEvent.None;
    private object resultData = null;
    
    public bool IsComplete => isComplete;
    public object Result => resultEvent;
    public object ResultData => resultData;
    
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
    }
    
    // New overload for completion with additional data
    protected void Complete(BattleEvent result, object data)
    {
        resultEvent = result;
        isComplete = true;
        resultData = data;
    }
}