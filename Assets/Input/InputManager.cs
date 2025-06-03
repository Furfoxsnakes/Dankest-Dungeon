using System;
using UnityEngine;
using GameInput;

public class InputManager : MonoBehaviour
{
    // Singleton instance
    public static InputManager Instance { get; private set; }
    
    // Events for battle actions
    public static event Action OnAttackPerformed;
    public static event Action OnDefendPerformed;
    public static event Action OnItemPerformed;
    public static event Action OnInventoryToggled;
    public static event Action OnSubmitPerformed;

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
        // Uncomment if you want this to persist between scenes
        DontDestroyOnLoad(gameObject);
        
        controls = new PlayerControls();
        
        // Set up inventory toggle binding
        controls.UI.OpenInventory.performed += ctx => ToggleInventory();
        
        // Set up the submit action
        controls.UI.Submit.performed += ctx => SubmitAction();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    // Public methods to be called by UI buttons
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
    
    public void SubmitAction()
    {
        OnSubmitPerformed?.Invoke();
    }
    
    // Method to enable just the battle input actions
    public void EnableBattleActions()
    {
        // controls.Battle.Enable();
        controls.UI.Submit.Enable();
    }
    
    // Method to disable just the battle input actions
    public void DisableBattleActions()
    {
        // controls.Battle.Disable();
        controls.UI.Submit.Disable();
    }
}