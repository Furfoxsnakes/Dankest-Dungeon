using System;

/// <summary>
/// Interface for states that can produce results
/// </summary>
public interface IState
{
    void Enter();
    void Exit();
    void Update();
    bool IsComplete { get; }
    object Result { get; }
}

/// <summary>
/// Generic state machine that handles state transitions based on results
/// </summary>
public class StateMachine<T> where T : class, IState
{
    private T currentState;
    
    public T CurrentState => currentState;
    public event Action<T, T> OnStateChanged;

    public void Initialize(T startingState)
    {
        currentState = startingState;
        currentState.Enter();
    }
    
    public void ChangeState(T newState)
    {
        if (currentState != newState)
        {
            T oldState = currentState;
            
            // Exit the old state
            currentState?.Exit();
            
            // Change state
            currentState = newState;
            
            // Enter the new state
            currentState.Enter();
            
            // Notify listeners
            OnStateChanged?.Invoke(oldState, currentState);
        }
    }
    
    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
    
    // Check if current state is complete and get its result
    public bool CheckStateCompletion(out object result)
    {
        if (currentState != null && currentState.IsComplete)
        {
            result = currentState.Result;
            return true;
        }
        
        result = null;
        return false;
    }
}