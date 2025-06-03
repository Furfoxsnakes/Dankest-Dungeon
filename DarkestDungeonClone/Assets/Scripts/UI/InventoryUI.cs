using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    private PlayerInput playerInput;

    void Awake()
    {
        playerInput = new PlayerInput();
    }

    void OnEnable()
    {
        playerInput.UI.Enable();
        playerInput.UI.OpenInventory.performed += OpenInventory;
    }

    void OnDisable()
    {
        playerInput.UI.Disable();
        playerInput.UI.OpenInventory.performed -= OpenInventory;
    }

    private void OpenInventory(InputAction.CallbackContext context)
    {
        // Logic to open the inventory UI
        Debug.Log("Inventory opened");
    }
}