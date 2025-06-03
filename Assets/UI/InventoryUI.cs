using UnityEngine;
using UnityEngine.InputSystem;
using GameInput;

public class InventoryUI : MonoBehaviour
{
    private PlayerControls controls;

    void Awake()
    {
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.UI.Enable();
        controls.UI.OpenInventory.performed += OpenInventory;
        InputManager.OnInventoryToggled += ShowInventory;
    }

    void OnDisable()
    {
        controls.UI.Disable();
        controls.UI.OpenInventory.performed -= OpenInventory;
        InputManager.OnInventoryToggled -= ShowInventory;
    }

    private void OpenInventory(InputAction.CallbackContext context)
    {
        // Logic to open the inventory UI
        Debug.Log("Inventory opened");
    }

    void ShowInventory()
    {
        // Logic to show/hide inventory
        Debug.Log("Toggle inventory visibility");
    }
}