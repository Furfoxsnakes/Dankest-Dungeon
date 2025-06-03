using UnityEngine;

/// <summary>
/// Base class for all character states
/// </summary>
public abstract class CharacterState : IState
{
    protected Character owner;
    protected bool isComplete;
    protected CharacterEvent resultEvent;
    
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
    
    // Changed to return CharacterEvent instead of object
    public object Result => resultEvent;
    
    // Helper method to complete the state with a specific event
    protected void Complete(CharacterEvent eventType)
    {
        isComplete = true;
        resultEvent = eventType;
    }
}