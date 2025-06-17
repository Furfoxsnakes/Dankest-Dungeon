using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DankestDungeon.Skills;
using System;
using TMPro;
using UnityEngine.EventSystems;
using DamageNumbersPro; 
using System.Linq; // Added for Linq operations like FirstOrDefault

// Placed here or in a separate file
[System.Serializable]
public struct ElementalColorMapping
{
    public ElementType elementType;
    public Color color;
}

public class BattleUI : MonoBehaviour
{
    public static BattleUI Instance { get; private set; }

    [Header("Skill Button UI")]
    [SerializeField] private GameObject skillButtonContainer; 
    [SerializeField] private GameObject skillButtonPrefab; 

    [Header("Skill Tooltip UI")]
    [SerializeField] private GameObject skillTooltipPanel; // Parent GameObject for the tooltip
    [SerializeField] private TextMeshProUGUI skillTooltipNameText;
    [SerializeField] private TextMeshProUGUI skillTooltipDescriptionText;
    [SerializeField] private TextMeshProUGUI skillTooltipManaCostText;
    // Add other fields like cooldown, accuracy, crit mod if needed

    [Header("Indicators")]
    [SerializeField] private TextMeshProUGUI activeCharacterNameText;
    [SerializeField] private GameObject targetIndicatorObject; 
    [SerializeField] private float activeCharacterIndicatorYOffset = 2.0f; 

    [Header("Damage Numbers Prefabs")]
    [SerializeField] private DamageNumber defaultDamageNumberPrefab; // Renamed for clarity
    [SerializeField] private DamageNumber criticalDamageNumberPrefab;
    [SerializeField] private DamageNumber criticalHealNumberPrefab;
    [SerializeField] private DamageNumber missNumberPrefab;
    [SerializeField] private DamageNumber dodgeNumberPrefab;
    [SerializeField] private DamageNumber blockNumberPrefab;
    [SerializeField] private Vector3 damageNumberWorldOffset = new Vector3(0, 1.5f, 0);

    [Header("Damage Numbers Colors")]
    [SerializeField] private List<ElementalColorMapping> elementalTypeColors = new List<ElementalColorMapping>();
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color defaultCritColor = Color.yellow; // Used if crit prefab doesn't define its own strong color
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color defaultCritHealColor = new Color(0.2f, 1f, 0.5f);
    [SerializeField] private Color statusEffectDamageColor = new Color(0.6f, 0.2f, 0.8f); // Purple
    [SerializeField] private Color statusEffectHealColor = new Color(0.4f, 0.8f, 0.4f); // Muted green
    [SerializeField] private Color defaultMissColor = Color.grey;
    [SerializeField] private Color defaultDodgeColor = new Color(0.5f, 0.8f, 1f); 
    [SerializeField] private Color defaultBlockColor = new Color(0.3f, 0.5f, 0.9f);
    [SerializeField] private Color defaultTextColor = Color.white; // Fallback for text popups if no other color is set


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If your BattleUI needs to persist across scene loads, though typically it's scene-specific.
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[BattleUI] Another instance of BattleUI already exists. Destroying this one.");
            Destroy(gameObject);
            return; // Important to return here to prevent further execution of Awake for this duplicate instance
        }

        if (skillButtonContainer != null)
        {
            skillButtonContainer.SetActive(false); 
        }
        else
        {
            Debug.LogError("[BattleUI] SkillButtonContainer is not assigned in the Inspector.");
        }

        if (skillButtonPrefab == null) 
        {
            Debug.LogError("[BattleUI] SkillButtonPrefab is not assigned in the Inspector. Cannot create skill buttons.");
        }

        if (activeCharacterNameText != null)
        {
            activeCharacterNameText.gameObject.SetActive(false);
        }

        if (targetIndicatorObject != null) 
        {
            targetIndicatorObject.SetActive(false); 
        }
        else 
        {
            Debug.LogWarning("[BattleUI] TargetIndicatorObject is not assigned in the Inspector. Target indication will not work.");
        }

        if (defaultDamageNumberPrefab == null) 
        {
            Debug.LogError("[BattleUI] DefaultDamageNumberPrefab (DamageNumbersPro) is not assigned. This is required for fallback.");
        }
        // Optional: Add warnings if specific prefabs are not set, as they will fallback to default.
        if (criticalDamageNumberPrefab == null) Debug.LogWarning("[BattleUI] CriticalDamageNumberPrefab not set. Will use default prefab for crits.");
        if (criticalHealNumberPrefab == null) Debug.LogWarning("[BattleUI] CriticalHealNumberPrefab not set. Will use default prefab for crit heals.");
        if (missNumberPrefab == null) Debug.LogWarning("[BattleUI] MissNumberPrefab not set. Will use default prefab for misses.");
        if (dodgeNumberPrefab == null) Debug.LogWarning("[BattleUI] DodgeNumberPrefab not set. Will use default prefab for dodges.");
        if (blockNumberPrefab == null) Debug.LogWarning("[BattleUI] BlockNumberPrefab not set. Will use default prefab for blocks.");

        // Initialize Skill Tooltip
        if (skillTooltipPanel != null)
        {
            skillTooltipPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[BattleUI] SkillTooltipPanel is not assigned. Skill tooltips will not be displayed.");
        }
        if (skillTooltipNameText == null) Debug.LogWarning("[BattleUI] SkillTooltipNameText is not assigned.");
        if (skillTooltipDescriptionText == null) Debug.LogWarning("[BattleUI] SkillTooltipDescriptionText is not assigned.");
        if (skillTooltipManaCostText == null) Debug.LogWarning("[BattleUI] SkillTooltipManaCostText is not assigned.");
    }

    void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.UIConfirmPerformed += HandleConfirmInput; 
            InputManager.UICancelPerformed += HandleCancelInput;   
        }
        else
        {
            Debug.LogError("[BattleUI] InputManager.Instance is null. Cannot subscribe to input events.");
        }
    }

    void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.UIConfirmPerformed -= HandleConfirmInput;
            InputManager.UICancelPerformed -= HandleCancelInput;
        }
    }

    private void HandleConfirmInput() 
    {
        Debug.Log("[BattleUI] Confirm Input (Select/Submit) performed via InputManager.");
    }

    private void HandleCancelInput()
    {
        Debug.Log("[BattleUI] Cancel Input performed via InputManager.");
    }

    public void ShowSkillButtons(Character character, Action<SkillDefinitionSO> onSkillSelectedCallback)
    {
        if (skillButtonContainer == null)
        {
            Debug.LogError("[BattleUI] SkillButtonContainer is not assigned. Cannot show skill buttons.");
            return;
        }
        if (skillButtonPrefab == null) 
        {
            Debug.LogError("[BattleUI] SkillButtonPrefab is not assigned. Cannot create skill buttons.");
            skillButtonContainer.SetActive(false);
            return;
        }

        Hero hero = character as Hero;
        if (hero == null)
        {
            Debug.LogError($"[BattleUI] Character {character?.GetName()} is not a Hero. Cannot display skills.");
            HideActionButtons();
            return;
        }

        List<SkillDefinitionSO> availableSkills = hero.Skills.GetAvailableSkills();

        if (availableSkills == null)
        {
            Debug.LogError($"[BattleUI] GetAvailableSkills() returned null for {hero.GetName()}. Hiding action buttons.");
            HideActionButtons();
            return;
        }
        
        foreach (Transform child in skillButtonContainer.transform)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[BattleUI] ShowSkillButtons called for {hero.GetName()}. Found {availableSkills.Count} skills. Attempting to activate skillButtonContainer.");
        skillButtonContainer.SetActive(true);
        HideSkillTooltip(); // Hide tooltip when repopulating buttons

        if (!skillButtonContainer.activeSelf)
        {
            Debug.LogError("[BattleUI] Attempted to activate skillButtonContainer, but it's still inactive. Check if its parent is inactive or if other scripts are deactivating it immediately.");
        }

        if (availableSkills.Count == 0)
        {
            Debug.LogWarning($"[BattleUI] {hero.GetName()} has 0 available skills. Skill panel will be empty.");
            EventSystem.current.SetSelectedGameObject(null); 
            return;
        }

        GameObject firstButtonGameObject = null; 

        for(int i = 0; i < availableSkills.Count; i++)
        {
            SkillDefinitionSO skill = availableSkills[i];
            if (skill == null)
            {
                Debug.LogWarning($"[BattleUI] Encountered a null skill in availableSkills for {hero.GetName()}. Skipping.");
                continue;
            }

            GameObject buttonGO = Instantiate(skillButtonPrefab, skillButtonContainer.transform);
            ActionButton actionButton = buttonGO.GetComponent<ActionButton>();

            if (actionButton == null)
            {
                Debug.LogError($"[BattleUI] SkillButtonPrefab '{skillButtonPrefab.name}' does not have an ActionButton component. Destroying instance.");
                Destroy(buttonGO);
                continue;
            }
            
            // Pass the hero (caster) to the ActionButton's setup if it needs it for the tooltip
            actionButton.Setup(skill, onSkillSelectedCallback, hero); 

            // In your ActionButton.cs, you would typically use IPointerEnterHandler/ISelectHandler:
            // public void OnPointerEnter(PointerEventData eventData) { BattleUI.Instance.ShowSkillTooltip(this.skillDefinition, this.casterHero); }
            // public void OnSelect(BaseEventData eventData) { BattleUI.Instance.ShowSkillTooltip(this.skillDefinition, this.casterHero); }
            // public void OnPointerExit(PointerEventData eventData) { BattleUI.Instance.HideSkillTooltip(); }
            // public void OnDeselect(BaseEventData eventData) { BattleUI.Instance.HideSkillTooltip(); }


            SkillRankData rankData = hero.Skills.GetSkillRankData(skill);
            bool canAffordThisSkill = false;
            if (rankData == null)
            {
                Debug.LogError($"[BattleUI] SkillRankData is null for skill {skill.skillNameKey} on {hero.GetName()}. Disabling button.");
                if (actionButton.button != null) actionButton.button.interactable = false;
            }
            else
            {
                int manaCost = rankData.manaCost;
                canAffordThisSkill = hero.CurrentMana >= manaCost;
                if (actionButton.button != null)
                {
                    actionButton.button.interactable = canAffordThisSkill;
                }
                else
                {
                    Debug.LogWarning($"[BattleUI] ActionButton for skill '{skill.skillNameKey}' does not have its 'button' field assigned. Cannot set interactability.");
                }
            }

            if (firstButtonGameObject == null && actionButton.button != null && actionButton.button.interactable)
            {
                firstButtonGameObject = actionButton.button.gameObject;
            }
        }

        if (firstButtonGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButtonGameObject);
            // Potentially show tooltip for the initially selected button here if desired,
            // though OnSelect should handle it if using EventSystem navigation.
            // ActionButton selectedActionButton = firstButtonGameObject.GetComponent<ActionButton>();
            // if (selectedActionButton != null) ShowSkillTooltip(selectedActionButton.skillDefinition, hero);

            Debug.Log($"[BattleUI] First skill button '{firstButtonGameObject.name}' selected.");
        }
        else if (availableSkills.Count > 0)
        {
             Debug.LogWarning("[BattleUI] No interactable skill buttons found to select as first.");
             EventSystem.current.SetSelectedGameObject(null); 
        }
    }

    public void HideActionButtons()
    {
        if (skillButtonContainer != null)
        {
            Debug.Log("[BattleUI] Hiding action buttons (skillButtonContainer) and clearing children.");
            foreach (Transform child in skillButtonContainer.transform) 
            {
                Destroy(child.gameObject);
            }
            skillButtonContainer.SetActive(false);
        }
        HideSkillTooltip(); // Also hide tooltip when action buttons are hidden
    }
    
    private void PositionAndShowIndicatorObject(Character characterToIndicate)
    {
        if (targetIndicatorObject == null)
        {
            Debug.LogWarning("[BattleUI] TargetIndicatorObject is not assigned. Cannot show/position indicator.");
            return;
        }

        if (characterToIndicate == null)
        {
            targetIndicatorObject.SetActive(false); 
            return;
        }

        targetIndicatorObject.transform.position = characterToIndicate.transform.position + Vector3.up * activeCharacterIndicatorYOffset;
        targetIndicatorObject.SetActive(true);
    }

    public void ShowActiveCharacterIndicator(Character character)
    {
        if (character == null)
        {
            Debug.LogWarning("[BattleUI] ShowActiveCharacterIndicator called with a null character.");
            HideActiveCharacterIndicator(); 
            return;
        }

        if (activeCharacterNameText != null)
        {
            activeCharacterNameText.text = $"Active: {character.GetName()}";
            activeCharacterNameText.gameObject.SetActive(true);
            Debug.Log($"[BattleUI] Active character name text set for: {character.GetName()}");
        }
        
        PositionAndShowIndicatorObject(character);
        Debug.Log($"[BattleUI] Active character indicator shown for: {character.GetName()}");
    }

    public void HideActiveCharacterIndicator()
    {
        if (activeCharacterNameText != null)
        {
            activeCharacterNameText.gameObject.SetActive(false);
        }
        if (targetIndicatorObject != null)
        {
            targetIndicatorObject.SetActive(false);
        }
        Debug.Log("[BattleUI] Hiding active character indicators (name and targetIndicatorObject).");
    }

    public void ShowTargetIndicator(Character target)
    {
        PositionAndShowIndicatorObject(target); 

        if (target != null)
        {
            Debug.Log($"[BattleUI] Target indicator shown on {target.GetName()} with Y offset: {activeCharacterIndicatorYOffset}");
        }
        else
        {
            Debug.Log("[BattleUI] Target indicator hidden (null target).");
        }
    }

    public void HideTargetIndicator()
    {
        if (targetIndicatorObject != null)
        {
            targetIndicatorObject.SetActive(false);
        }
        Debug.Log("[BattleUI] Hiding target indicator object.");
    }

    public void ShowSkillTooltip(SkillDefinitionSO skillDefinition, Character caster)
    {
        if (skillTooltipPanel == null || skillDefinition == null || caster == null)
        {
            HideSkillTooltip(); // Hide if essential components are missing
            if (skillTooltipPanel == null) Debug.LogWarning("[BattleUI] SkillTooltipPanel is null. Cannot show tooltip.");
            if (skillDefinition == null) Debug.LogWarning("[BattleUI] SkillDefinition is null. Cannot show tooltip.");
            if (caster == null) Debug.LogWarning("[BattleUI] Caster is null. Cannot retrieve rank data for tooltip.");
            return;
        }

        Hero hero = caster as Hero;
        if (hero == null)
        {
            Debug.LogWarning($"[BattleUI] Caster {caster.GetName()} is not a Hero. Cannot get skill rank data for tooltip.");
            HideSkillTooltip();
            return;
        }

        SkillRankData rankData = hero.Skills.GetSkillRankData(skillDefinition);
        if (rankData == null)
        {
            Debug.LogError($"[BattleUI] Could not find SkillRankData for skill '{skillDefinition.skillNameKey}' on hero '{hero.GetName()}'. Hiding tooltip.");
            HideSkillTooltip();
            return;
        }

        if (skillTooltipNameText != null)
        {
            // Assuming you have a LocalizationManager, otherwise use skillDefinition.skillNameKey directly
            // skillTooltipNameText.text = LocalizationManager.Instance.GetLocalizedValue(skillDefinition.skillNameKey);
            skillTooltipNameText.text = skillDefinition.skillNameKey; // Placeholder
        }

        if (skillTooltipDescriptionText != null)
        {
            // Assuming you have a LocalizationManager, otherwise use rankData.editorDescriptionPreview
            // skillTooltipDescriptionText.text = LocalizationManager.Instance.GetLocalizedValue(rankData.rankDescriptionKey);
            skillTooltipDescriptionText.text = rankData.editorDescriptionPreview; // Placeholder
        }
        
        if (skillTooltipManaCostText != null)
        {
            skillTooltipManaCostText.text = $"Mana: {rankData.manaCost}";
        }

        // You can add more details here:
        // if (skillTooltipCooldownText != null) skillTooltipCooldownText.text = $"Cooldown: {rankData.cooldown}";
        // if (skillTooltipAccuracyText != null) skillTooltipAccuracyText.text = $"ACC Mod: {rankData.accuracyMod}%";
        // if (skillTooltipCritText != null) skillTooltipCritText.text = $"Crit Mod: {rankData.critMod}%";

        skillTooltipPanel.SetActive(true);
        Debug.Log($"[BattleUI] Showing tooltip for skill: {skillDefinition.skillNameKey}");
    }

    public void HideSkillTooltip()
    {
        if (skillTooltipPanel != null)
        {
            skillTooltipPanel.SetActive(false);
        }
        Debug.Log("[BattleUI] Hiding skill tooltip.");
    }

    public void ShowDamageNumber(Character target, int amount, DamageNumberType type, ElementType elementType = ElementType.Physical)
    {
        Debug.LogWarning($"[BattleUI.ShowDamageNumber ENTRY] Target: {(target ? target.name : "NULL")}, Amount: {amount}, Type: {type}, Element: {elementType}");

        if (defaultDamageNumberPrefab == null) // Check the default prefab first
        {
            Debug.LogError("[BattleUI] DefaultDamageNumberPrefab (DamageNumbersPro) is not assigned. Cannot show damage numbers.");
            return;
        }
        if (target == null)
        {
            Debug.LogError("[BattleUI] Target is null. Cannot show damage number.");
            return;
        }

        Vector3 spawnPosition = target.transform.position + damageNumberWorldOffset;
        DamageNumber prefabToUse = defaultDamageNumberPrefab;
        DamageNumber spawnedNumber = null;
        string displayText = ""; 
        bool isTextPopup = false;

        // 1. Select Prefab based on DamageNumberType
        switch (type)
        {
            case DamageNumberType.CriticalDamage:
                prefabToUse = criticalDamageNumberPrefab != null ? criticalDamageNumberPrefab : defaultDamageNumberPrefab;
                break;
            case DamageNumberType.CriticalHeal:
                prefabToUse = criticalHealNumberPrefab != null ? criticalHealNumberPrefab : defaultDamageNumberPrefab;
                break;
            case DamageNumberType.Miss:
                prefabToUse = missNumberPrefab != null ? missNumberPrefab : defaultDamageNumberPrefab;
                displayText = "Miss";
                isTextPopup = true;
                break;
            case DamageNumberType.Dodge:
                prefabToUse = dodgeNumberPrefab != null ? dodgeNumberPrefab : defaultDamageNumberPrefab;
                displayText = "Dodge";
                isTextPopup = true;
                break;
            case DamageNumberType.Block:
                prefabToUse = blockNumberPrefab != null ? blockNumberPrefab : defaultDamageNumberPrefab;
                displayText = "Block";
                isTextPopup = true;
                break;
            // For other types, we'll use defaultDamageNumberPrefab
        }

        // 2. Determine Color
        Color chosenColor = defaultTextColor; // Fallback for text or uncolored numbers

        // Attempt to get elemental color first
        ElementalColorMapping elementalMapping = elementalTypeColors.FirstOrDefault(ec => ec.elementType == elementType);
        bool elementalColorFound = elementalMapping.elementType == elementType; // Check if a mapping was actually found

        if (elementalColorFound && elementType != ElementType.Physical && elementType != ElementType.Healing) // Don't override physical/healing with elemental if they have specific type colors
        {
            chosenColor = elementalMapping.color;
        }
        else // Fallback to type-based colors if no specific elemental color or if it's Physical/Healing
        {
            switch (type)
            {
                case DamageNumberType.NormalDamage:
                case DamageNumberType.StatusEffectDamage: // Can share color or have its own
                    chosenColor = elementalColorFound && elementType == ElementType.Physical ? elementalMapping.color : // Use physical elemental color if set
                                  (type == DamageNumberType.StatusEffectDamage ? statusEffectDamageColor : normalDamageColor);
                    break;
                case DamageNumberType.CriticalDamage:
                     chosenColor = elementalColorFound ? elementalMapping.color : defaultCritColor;
                    break;
                case DamageNumberType.Heal:
                case DamageNumberType.StatusEffectHeal:
                     chosenColor = elementalColorFound && elementType == ElementType.Healing ? elementalMapping.color : // Use healing elemental color if set
                                  (type == DamageNumberType.StatusEffectHeal ? statusEffectHealColor : healColor);
                    break;
                case DamageNumberType.CriticalHeal:
                    chosenColor = elementalColorFound && elementType == ElementType.Healing ? elementalMapping.color : defaultCritHealColor;
                    break;
                case DamageNumberType.Miss:
                    chosenColor = defaultMissColor;
                    break;
                case DamageNumberType.Dodge:
                    chosenColor = defaultDodgeColor;
                    break;
                case DamageNumberType.Block:
                    chosenColor = defaultBlockColor;
                    break;
            }
        }


        // 3. Spawn the number/text
        if (isTextPopup)
        {
            spawnedNumber = prefabToUse.Spawn(spawnPosition, displayText);
        }
        else
        {
            spawnedNumber = prefabToUse.Spawn(spawnPosition, amount);
        }

        // 4. Apply Color and Log
        if (spawnedNumber != null)
        {
            spawnedNumber.SetColor(chosenColor);
            if (isTextPopup)
            {
                 Debug.Log($"[BattleUI] Spawned DNP Text: '{displayText}' (Type: {type}, Element: {elementType}) on {target.GetName()} with prefab '{prefabToUse.name}' and color {chosenColor}");
            }
            else
            {
                 Debug.Log($"[BattleUI] Spawned DNP Number: {amount} (Type: {type}, Element: {elementType}) on {target.GetName()} with prefab '{prefabToUse.name}' and color {chosenColor}");
            }
        }
        else
        {
            Debug.LogError($"[BattleUI] Failed to spawn DamageNumberPro for type {type}, element {elementType} on {target.GetName()} using prefab '{prefabToUse.name}'.");
        }
    }
}