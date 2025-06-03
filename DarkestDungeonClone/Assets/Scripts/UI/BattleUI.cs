using UnityEngine;
using UnityEngine.InputSystem;

public class BattleUI : MonoBehaviour
{
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = new PlayerInput();
    }

    void OnEnable()
    {
        playerInput.Enable();
        playerInput.Battle.Select.performed += OnSelect;
        playerInput.Battle.Cancel.performed += OnCancel;
    }

    void OnDisable()
    {
        playerInput.Battle.Select.performed -= OnSelect;
        playerInput.Battle.Cancel.performed -= OnCancel;
        playerInput.Disable();
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        // Handle selection logic here
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        // Handle cancel logic here
    }
}