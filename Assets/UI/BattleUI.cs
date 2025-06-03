using UnityEngine;
using UnityEngine.InputSystem;
using GameInput;

public class BattleUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    
    [Header("World Space Elements")]
    [SerializeField] private GameObject worldSpaceIndicator; // Direct GameObject, not UI element
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.5f, 0f);
    
    [Header("Screen Space UI Elements")]
    [SerializeField] private Canvas battleUICanvas;
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private GameObject partyInfoPanel;
    
    private Character currentActiveCharacter;
    private PlayerControls playerControls;
    private Camera battleCamera;

    void Awake()
    {
        playerControls = new PlayerControls();
        battleCamera = Camera.main;
        
        // Start with indicator hidden
        HideActiveCharacterIndicator();
    }

    void Start()
    {
        // Fallback if not assigned in inspector
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();
    }

    void OnEnable()
    {
        playerControls.Enable();
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;
    }

    void OnDisable()
    {
        playerControls.UI.Submit.performed -= OnSubmit;
        playerControls.UI.Cancel.performed -= OnCancel;
        playerControls.Disable();
    }
    
    void Update()
    {
        // Update indicator position if it's active and we have a character
        if (worldSpaceIndicator.activeSelf && currentActiveCharacter != null)
        {
            UpdateIndicatorPosition();
        }
    }
    
    // Position the indicator above the character's head
    private void UpdateIndicatorPosition()
    {
        // Directly position in world space - much simpler!
        worldSpaceIndicator.transform.position = currentActiveCharacter.transform.position + indicatorOffset;
    }
    
    // Public methods to show and hide the indicator
    public void ShowActiveCharacterIndicator(Character character)
    {
        currentActiveCharacter = character;
        worldSpaceIndicator.SetActive(true);
        UpdateIndicatorPosition();
    }
    
    public void HideActiveCharacterIndicator()
    {
        worldSpaceIndicator.SetActive(false);
        currentActiveCharacter = null;
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        // Handle selection logic here
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        // Handle cancel logic here
    }
    
    private void ShowActionMenu()
    {
        // Standard UI operations
        actionPanel.SetActive(true);
    }
}