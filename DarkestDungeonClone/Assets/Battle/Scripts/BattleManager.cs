using UnityEngine;
using UnityEngine.InputSystem;

public class BattleManager : MonoBehaviour
{
    private InputActionAsset inputActions;
    private InputAction attackAction;
    private InputAction defendAction;

    void Awake()
    {
        inputActions = Resources.Load<InputActionAsset>("Input/InputActions");
        attackAction = inputActions.FindAction("Attack");
        defendAction = inputActions.FindAction("Defend");
    }

    void OnEnable()
    {
        attackAction.Enable();
        defendAction.Enable();
    }

    void OnDisable()
    {
        attackAction.Disable();
        defendAction.Disable();
    }

    void Start()
    {
        // Initialize battle state
    }

    void Update()
    {
        if (attackAction.triggered)
        {
            // Handle attack logic
        }

        if (defendAction.triggered)
        {
            // Handle defend logic
        }
    }
}