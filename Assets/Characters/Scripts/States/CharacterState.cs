using UnityEngine;

/// <summary>
/// Base class for all character states
/// </summary>
public abstract class CharacterState : IState // IState now includes ResetCompletionStatus
{
    protected Character owner;
    protected bool isComplete;
    protected CharacterEvent resultEvent; // This is already specific, which is good.
    
    public CharacterState(Character character)
    {
        owner = character;
        isComplete = false;
        resultEvent = CharacterEvent.None;
    }
    
    public virtual void Enter() { }
    
    public virtual void Exit() { }
    
    public virtual void Update() { }
    
    public bool IsComplete => isComplete;
    
    // Result now directly uses the specific CharacterEvent type.
    // The IState.Result still returns object, this is fine due to covariance if CharacterEvent is a class,
    // or implicit conversion if it's a struct.
    // If CharacterEvent is an enum (value type), this direct return is fine for the concrete class.
    // The IState interface's 'object Result' will box it.
    public object Result => resultEvent; 
    
    // Helper method to complete the state with a specific event
    protected void Complete(CharacterEvent eventType)
    {
        isComplete = true;
        resultEvent = eventType;
        // Debug.Log($"[CharacterState] {this.GetType().Name} for {owner.GetName()} completed with {eventType}");
    }

    // Implementation of ResetCompletionStatus from IState
    public virtual void ResetCompletionStatus()
    {
        isComplete = false;
        resultEvent = CharacterEvent.None; // Reset to a default event
        // Debug.Log($"[CharacterState] {this.GetType().Name} for {owner.GetName()} completion status reset.");
    }
}