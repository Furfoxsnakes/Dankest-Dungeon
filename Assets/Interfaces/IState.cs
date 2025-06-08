/// <summary>
/// Interface for states that can be managed by a StateMachine.
/// States can produce results and have their completion status reset.
/// </summary>
public interface IState
{
    void Enter();
    void Exit();
    void Update();
    bool IsComplete { get; }
    object Result { get; } // Consider if this should be more specific if all states return a similar event type
    void ResetCompletionStatus(); // Added from IResettableCompletion
}