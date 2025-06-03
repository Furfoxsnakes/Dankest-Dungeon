using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (playerInput.Gameplay.Attack.triggered)
        {
            // Handle attack input
        }

        if (playerInput.Gameplay.Defend.triggered)
        {
            // Handle defend input
        }

        if (playerInput.Gameplay.Item.triggered)
        {
            // Handle item usage input
        }
    }
}