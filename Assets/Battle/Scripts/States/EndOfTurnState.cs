using UnityEngine;

public class EndOfTurnState : BattleState
{
    private float waitDuration; // Duration to wait in seconds
    private float timer;

    public EndOfTurnState(BattleManager manager, float delay) : base(manager) 
    {
        this.waitDuration = delay;
    }

    public override void Enter()
    {
        Debug.Log("<color=cyan>[EndOfTurnState] Enter. Waiting for " + waitDuration + " seconds.</color>");
        timer = 0f;
        if (waitDuration <= 0) // If delay is zero or negative, complete immediately
        {
            Debug.Log("<color=cyan>[EndOfTurnState] Wait duration is zero or less. Signaling ActionFullyComplete immediately.</color>");
            Complete(BattleEvent.ActionFullyComplete);
        }
    }

    public override void Update()
    {
        if (IsComplete) return; // Don't update if already completed (e.g., due to zero delay)

        timer += Time.deltaTime;
        if (timer >= waitDuration)
        {
            Debug.Log("<color=cyan>[EndOfTurnState] Wait complete. Signaling ActionFullyComplete.</color>");
            Complete(BattleEvent.ActionFullyComplete);
        }
    }

    public override void Exit()
    {
        Debug.Log("<color=cyan>[EndOfTurnState] Exit.</color>");
    }
}