using System;
using UnityEngine;
using UnityEngine.InputSystem; // Required for InputAction.CallbackContext
using GameInput;

public class InputManager : MonoBehaviour
{
    // Singleton instance
    public static InputManager Instance { get; private set; }
    
    // --- Existing Events for UI Button Clicks ---
    public static event Action OnAttackPerformed; // Keep if UI buttons call PerformAttack()
    public static event Action OnDefendPerformed; // Keep if UI buttons call PerformDefend()
    public static event Action OnItemPerformed;   // Keep if UI buttons call PerformItem()
    public static event Action OnInventoryToggled;
    public static event Action OnSubmitPerformed; // Keep if UI buttons call SubmitAction()

    // --- New Static Events for Direct Input Actions ---
    public static event Action<Vector2> UINavigatePerformed;
    public static event Action UIConfirmPerformed;
    public static event Action UICancelPerformed;


    // Reference to player input actions
    private PlayerControls controls;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep if you want it to persist
        
        controls = new PlayerControls();
        
        // --- Set up UI Action Map bindings for new static events ---
        controls.UI.Navigate.performed += HandleNavigate;
        controls.UI.Submit.performed += HandleConfirm; // This will now also fire UIConfirmPerformed
        controls.UI.Cancel.performed += HandleCancel;

        // Set up inventory toggle binding (existing)
        controls.UI.OpenInventory.performed += ctx => ToggleInventory(); // ToggleInventory invokes OnInventoryToggled
    }

    private void OnEnable()
    {
        controls.Enable(); // Enable all action maps by default, or manage specific maps
        // Example: controls.UI.Enable(); controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // --- Input Action Handlers ---
    private void HandleNavigate(InputAction.CallbackContext context)
    {
        UINavigatePerformed?.Invoke(context.ReadValue<Vector2>());
    }

    private void HandleConfirm(InputAction.CallbackContext context)
    {
        // This will be invoked by the UI.Submit action
        UIConfirmPerformed?.Invoke();
        // Also, keep the old OnSubmitPerformed if UI buttons call SubmitAction() directly
        // OnSubmitPerformed?.Invoke(); // This line might be redundant if SubmitAction() is the sole invoker of OnSubmitPerformed
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        UICancelPerformed?.Invoke();
    }


    // --- Public methods to be called by UI buttons (existing) ---
    public void PerformAttack()
    {
        OnAttackPerformed?.Invoke();
    }

    public void PerformDefend()
    {
        OnDefendPerformed?.Invoke();
    }

    public void PerformItem()
    {
        OnItemPerformed?.Invoke();
    }

    public void ToggleInventory()
    {
        OnInventoryToggled?.Invoke();
    }
    
    // This method can still be called by UI buttons if needed,
    // but direct input for "Submit" will now also trigger UIConfirmPerformed.
    public void SubmitAction()
    {
        OnSubmitPerformed?.Invoke();
        // Optionally, also fire the general confirm event if a button press should act like direct input
        // UIConfirmPerformed?.Invoke(); 
    }
    
    // --- Methods to enable/disable specific action maps (examples) ---
    public void EnableUIControls()
    {
        controls.Player.Disable(); // Assuming you have a "Player" map for movement
        controls.UI.Enable();
    }

    public void EnablePlayerControls()
    {
        controls.UI.Disable();
        controls.Player.Enable(); // Assuming you have a "Player" map for movement
    }
    
    public void EnableAllControls()
    {
        controls.Enable();
    }

    public void DisableAllControls()
    {
        controls.Disable();
    }

    // Method to enable just the battle input actions (adjust as needed)
    // This might mean enabling the UI map.
    public void EnableBattleActions()
    {
        // controls.Battle.Enable(); // If you have a specific "Battle" map
        controls.UI.Enable(); // Usually, battle actions are UI-driven
    }
    
    // Method to disable just the battle input actions
    public void DisableBattleActions()
    {
        // controls.Battle.Disable();
        // controls.UI.Disable(); // Or be more granular
    }
}