using UnityEngine;
using UnityEngine.UI; // Required for Button
using UnityEngine.EventSystems; // Required for EventSystem
using UnityEngine.InputSystem;
using GameInput; // Assuming your PlayerControls is in this namespace
using System.Collections.Generic; // For List
using DankestDungeon.Skills;
using System;
using DamageNumbersPro;   // For SkillDefinitionSO
using TMPro; // If your DamageNumberGUI prefab uses TextMeshPro for display, otherwise remove


public class BattleUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager; // Assign in Inspector
    [SerializeField] private InputManager inputManager; // Assign in Inspector or it will try to find Instance

    [Header("World Space Elements")]
    [SerializeField] private GameObject worldSpaceIndicator; // This will be used for both active character and target
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Screen Space UI Elements")]
    [SerializeField] private Canvas battleUICanvas;
    [SerializeField] private GameObject actionButtonsPanel; // Panel containing your action buttons
    [SerializeField] private GameObject partyInfoPanel;
    // [SerializeField] private DamageNumber NormalHitDamageNumberPrefab; // This will be replaced by more specific ones

    [Header("Damage Number Prefabs")]
    [SerializeField] private DamageNumber normalDamagePrefab;
    [SerializeField] private DamageNumber criticalDamagePrefab;
    [SerializeField] private DamageNumber healPrefab;
    [SerializeField] private DamageNumber criticalHealPrefab;
    [SerializeField] private DamageNumber fireDamagePrefab;
    [SerializeField] private DamageNumber criticalFireDamagePrefab;
    // Add more elemental prefabs as needed (e.g., iceDamagePrefab, poisonTickPrefab)

    [Header("Action Buttons")]
    [SerializeField] private GameObject actionButtonPrefab; // Assign your ActionButton prefab
    [SerializeField] private Transform actionButtonsContainer; // Parent where buttons will be instantiated (e.g., actionButtonsPanel itself or a child LayoutGroup)

    private Character currentCharacterForIndicator; // Renamed for clarity, this is what the indicator follows
    private PlayerControls playerControls;
    private Camera battleCamera;

    private List<GameObject> _instantiatedActionButtons = new List<GameObject>();

    // Enum to differentiate damage/heal types for future styling
    public enum DamageNumberType
    {
        NormalDamage,
        CriticalDamage,
        Heal,
        CriticalHeal,
        StatusEffect,
        FireDamage,
        CriticalFireDamage,
        IceDamage, // Example for future
        CriticalIceDamage, // Example for future
        PoisonTick, // Example for future
        Miss
        // Add more as needed
    }

    void Awake()
    {
        playerControls = new PlayerControls();
        battleCamera = Camera.main;

        if (actionButtonsContainer == null && actionButtonsPanel != null)
        {
            actionButtonsContainer = actionButtonsPanel.transform; // Default to panel itself
        }
        if (actionButtonsPanel != null) actionButtonsPanel.SetActive(false);
        
        // Ensure the single indicator is hidden initially and not following anyone
        if (worldSpaceIndicator != null)
        {
            worldSpaceIndicator.SetActive(false);
        }
        currentCharacterForIndicator = null;

        // Assign button listeners (if not done in Inspector)
        // if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonPressed);
        // if (defendButton != null) defendButton.onClick.AddListener(OnDefendButtonPressed);
        // if (itemButton != null) itemButton.onClick.AddListener(OnItemButtonPressed);

        if (inputManager == null)
        {
            inputManager = InputManager.Instance;
            if (inputManager == null) Debug.LogError("InputManager instance not found for BattleUI.");
        }
    }

    void Start()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager == null) Debug.LogError("BattleManager not found for BattleUI.");
    }

    void OnEnable()
    {
        playerControls.UI.Enable();
        // playerControls.UI.Cancel.performed += OnCancelButtonPressed;
    }

    void OnDisable()
    {
        // playerControls.UI.Cancel.performed -= OnCancelButtonPressed;
        playerControls.UI.Disable();
    }

    void Update()
    {
        // If the indicator is active and has a character to follow, update its position
        if (worldSpaceIndicator != null && worldSpaceIndicator.activeSelf && currentCharacterForIndicator != null)
        {
            UpdateIndicatorPosition();
        }
    }

    private void UpdateIndicatorPosition()
    {
        // This method now uses currentCharacterForIndicator
        if (currentCharacterForIndicator != null && worldSpaceIndicator != null)
        {
            worldSpaceIndicator.transform.position = currentCharacterForIndicator.transform.position + indicatorOffset;
        }
    }

    public void ShowActiveCharacterIndicator(Character character)
    {
        this.currentCharacterForIndicator = character; // Set who the indicator should follow
        if (worldSpaceIndicator != null)
        {
            worldSpaceIndicator.SetActive(true);
            UpdateIndicatorPosition(); // Position it once immediately
        }
    }

    public void HideActiveCharacterIndicator()
    {
        if (worldSpaceIndicator != null)
        {
            worldSpaceIndicator.SetActive(false);
        }
        // Don't nullify currentCharacterForIndicator here if another state (like TargetSelection) might be using it.
        // Let the state that *sets* the character also be responsible for clearing or hiding.
        // However, if this is a generic "hide everything related to active char", then nullifying is okay.
        // For now, let's assume states manage who currentCharacterForIndicator is.
        // If PlayerTurnState calls this on exit, it's fine.
    }

    // ---- Modified Target Indicator Methods (now use worldSpaceIndicator) ----
    public void ShowTargetIndicator(Character targetCharacter)
    {
        this.currentCharacterForIndicator = targetCharacter; // The indicator will now follow this target
        if (worldSpaceIndicator != null)
        {
            worldSpaceIndicator.SetActive(true);
            UpdateIndicatorPosition(); // Position it once immediately on the target
        }
        else if (worldSpaceIndicator == null) // Corrected from targetIndicator
        {
            Debug.LogWarning("worldSpaceIndicator is not assigned in BattleUI.");
        }
    }

    public void HideTargetIndicator()
    {
        // This method simply hides the indicator.
        // The state transitioning out of target selection (TargetSelectionState.Exit) will call this.
        // The next state (e.g., PlayerTurnState.Enter or ActionExecutionState.Enter)
        // will decide if and where the indicator should be shown next.
        if (worldSpaceIndicator != null)
        {
            worldSpaceIndicator.SetActive(false);
        }
        // We don't necessarily null out currentCharacterForIndicator here,
        // as the next state will set it if needed.
    }
    // ---- End Modified Target Indicator Methods ----

    public void ShowActionButtons(bool show)
    {
        if (actionButtonsPanel == null) return;

        actionButtonsPanel.SetActive(show);
        if (show)
        {
            if (_instantiatedActionButtons.Count > 0)
            {
                EventSystem.current.SetSelectedGameObject(null); // Deselect any current selection
                EventSystem.current.SetSelectedGameObject(_instantiatedActionButtons[0]); // Select the first button
            }
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null); 
        }
    }

    // New method to show skill buttons
    public void ShowSkillButtons(Character character, Action<SkillDefinitionSO> onSkillSelectedCallback)
    {
        if (actionButtonsPanel == null || actionButtonPrefab == null || actionButtonsContainer == null)
        {
            Debug.LogError("BattleUI is missing references for action buttons panel, prefab, or container.");
            return;
        }

        ClearActionButtons(); // Clear previous buttons

        if (character == null)
        {
            Debug.LogWarning("ShowSkillButtons called with a null character.");
            actionButtonsPanel.SetActive(false);
            return;
        }

        var learnedSkills = character.LearnedSkills; // From Character.cs
        if (learnedSkills.Count == 0)
        {
            Debug.Log($"{character.GetName()} has no learned skills.");
            actionButtonsPanel.SetActive(false);
            // Optionally, show a "Pass" or "Basic Attack" button here
            return;
        }

        actionButtonsPanel.SetActive(true);
        bool firstButton = true;

        foreach (var skillEntry in learnedSkills)
        {
            SkillDefinitionSO skillDef = skillEntry.Key;
            // TODO: Add checks here: Can the character use this skill from their current rank? Enough mana? Cooldown?
            // For now, we display all learned skills.

            GameObject buttonGO = Instantiate(actionButtonPrefab, actionButtonsContainer);
            ActionButton buttonUI = buttonGO.GetComponent<ActionButton>();

            if (buttonUI != null)
            {
                buttonUI.Setup(skillDef, onSkillSelectedCallback);
                _instantiatedActionButtons.Add(buttonGO);

                if (firstButton && buttonGO.GetComponent<Button>() != null)
                {
                    EventSystem.current.SetSelectedGameObject(null); // Deselect first
                    EventSystem.current.SetSelectedGameObject(buttonGO); // Select the first skill button
                    firstButton = false;
                }
            }
            else
            {
                Debug.LogError("ActionButton prefab is missing the ActionButtonUI script.", buttonGO);
                Destroy(buttonGO);
            }
        }
        if (_instantiatedActionButtons.Count == 0)
        {
             actionButtonsPanel.SetActive(false); // Hide panel if no valid skills were added
        }
    }

    public void HideActionButtons()
    {
        if (actionButtonsPanel != null)
        {
            actionButtonsPanel.SetActive(false);
        }
        ClearActionButtons();
        if (EventSystem.current != null) // Check if EventSystem still exists (e.g. during scene changes)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void ClearActionButtons()
    {
        foreach (GameObject btn in _instantiatedActionButtons)
        {
            Destroy(btn);
        }
        _instantiatedActionButtons.Clear();
    }

    // These methods might be deprecated or changed if all actions are skills
    public void OnAttackButtonPressed()
    {
        // This would now likely be a specific "Attack" skill
        // battleManager?.PlayerSelectedActionType(ActionType.Attack); 
        Debug.LogWarning("OnAttackButtonPressed is likely deprecated. Use skill system.");
    }

    public void OnDefendButtonPressed()
    {
        // This could be a specific "Defend" skill
        // battleManager?.PlayerSelectedActionType(ActionType.Defend);
        Debug.LogWarning("OnDefendButtonPressed is likely deprecated. Use skill system.");
    }

    private void OnCancelButtonPressed(InputAction.CallbackContext context)
    {
        if (actionButtonsPanel != null && actionButtonsPanel.activeSelf)
        {
            Debug.Log("BattleUI: Cancel button pressed while action buttons active, hiding them.");
            ShowActionButtons(false); 
            // Consider if this should also signal BattleManager to return to a previous step in PlayerTurnState,
            // e.g., if there was a "main menu" before action buttons.
            // For now, it just hides the buttons. PlayerTurnState's cancel logic (if any) would handle state changes.
        }
        // Note: TargetSelectionState has its own Cancel handling. This one is for the action button panel.
    }

    // ---- New Method to Show Damage Numbers ----
    public void ShowDamageNumber(Character target, int amount, DamageNumberType type = DamageNumberType.NormalDamage)
    {
        if (target == null)
        {
            Debug.LogWarning("ShowDamageNumber called with a null target.");
            return;
        }

        DamageNumber prefabToSpawn = null;

        switch (type)
        {
            case DamageNumberType.NormalDamage:
                prefabToSpawn = normalDamagePrefab;
                break;
            case DamageNumberType.CriticalDamage:
                prefabToSpawn = criticalDamagePrefab != null ? criticalDamagePrefab : normalDamagePrefab;
                break;
            case DamageNumberType.Heal:
                prefabToSpawn = healPrefab;
                break;
            case DamageNumberType.CriticalHeal:
                prefabToSpawn = criticalHealPrefab != null ? criticalHealPrefab : healPrefab;
                break;
            case DamageNumberType.FireDamage:
                prefabToSpawn = fireDamagePrefab != null ? fireDamagePrefab : normalDamagePrefab; // Fallback to normal if specific not set
                break;
            case DamageNumberType.CriticalFireDamage:
                prefabToSpawn = criticalFireDamagePrefab != null ? criticalFireDamagePrefab : (fireDamagePrefab != null ? fireDamagePrefab : criticalDamagePrefab); // Fallback chain
                break;
            // Add cases for IceDamage, CriticalIceDamage, PoisonTick, Miss, etc.
            // case DamageNumberType.IceDamage:
            //     prefabToSpawn = iceDamagePrefab != null ? iceDamagePrefab : normalDamagePrefab;
            //     break;
            // case DamageNumberType.PoisonTick:
            //     prefabToSpawn = poisonTickPrefab != null ? poisonTickPrefab : normalDamagePrefab;
            //     break;
            // case DamageNumberType.Miss:
            //     // Handle text like "Miss" - DamageNumbersPro might have a different way to spawn text-only popups
            //     // For now, assuming it uses a DamageNumber prefab that just shows text.
            //     // prefabToSpawn = missTextPrefab; // You'd need a 'missTextPrefab' field
            //     // if (missTextPrefab != null) {
            //     //    missTextPrefab.Spawn(target.transform.position + new Vector3(0, 0.25f, 0), "Miss"); // Or however DNP handles text
            //     //    return; // Early exit if handled differently
            //     // }
            //     break;
            default:
                Debug.LogWarning($"No specific prefab configured for DamageNumberType '{type}'. Falling back to normal damage prefab.");
                prefabToSpawn = normalDamagePrefab;
                break;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError($"Fallback prefab (normalDamagePrefab) is not assigned in BattleUI, or selected prefab for type '{type}' is null.");
            return;
        }

        Vector3 spawnPosition = target.transform.position + new Vector3(0, 0.25f, 0);
        var spawnedNumber = prefabToSpawn.Spawn(spawnPosition, amount);

        if (spawnedNumber == null)
        {
            Debug.LogError($"Failed to spawn damage number for target {target.GetName()} with amount {amount} using prefab {prefabToSpawn.name}. Check DamageNumbersPro setup.");
        }
    }
    // ---- End New Method ----
}