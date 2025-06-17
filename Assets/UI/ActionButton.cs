using UnityEngine;
using UnityEngine.UI;
using TMPro; // If you use TextMeshPro for skill names
using DankestDungeon.Skills;
using System; // For Action
using UnityEngine.EventSystems; // Required for hover/selection events

public class ActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Button button;
    public Image iconImage;
    public TextMeshProUGUI nameText; // Optional: Assign if you want to display skill name

    private SkillDefinitionSO _skillDefinition;
    private Action<SkillDefinitionSO> _onClickAction;
    private Hero _caster; // Store the caster for tooltip context

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (iconImage == null && transform.Find("Icon") != null) iconImage = transform.Find("Icon").GetComponent<Image>();
        if (nameText == null && transform.Find("NameText") != null) nameText = transform.Find("NameText").GetComponent<TextMeshProUGUI>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError("Button component not found on ActionButtonUI prefab.", this);
        }
    }

    public void Setup(SkillDefinitionSO skillDef, Action<SkillDefinitionSO> onClickCallback, Hero caster)
    {
        _skillDefinition = skillDef;
        _onClickAction = onClickCallback;
        _caster = caster; // Store the caster

        if (skillDef == null)
        {
            Debug.LogError("SkillDefinitionSO is null in ActionButtonUI.Setup", this);
            if(iconImage != null) iconImage.enabled = false;
            if(nameText != null) nameText.text = "Error";
            button.interactable = false; // Disable button if skill is null
            return;
        }
        else
        {
            button.interactable = true; // Ensure button is interactable if skill is valid
        }


        if (iconImage != null && skillDef.icon != null)
        {
            iconImage.sprite = skillDef.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        if (nameText != null)
        {
            nameText.text = skillDef.skillNameKey; // Ideally, this would be localized
            nameText.enabled = true;
        }
        else if (nameText != null) // This else if seems redundant if nameText is null
        {
            nameText.enabled = false;
        }
    }

    private void OnButtonClicked()
    {
        if (_skillDefinition == null)
        {
            Debug.LogError("Attempted to click button with null SkillDefinitionSO.", this);
            return;
        }
        _onClickAction?.Invoke(_skillDefinition);
        if (BattleUI.Instance != null) // Use the singleton instance
        {
            BattleUI.Instance.HideSkillTooltip();
        }
    }

    // --- EventSystem Handlers for Tooltip ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (BattleUI.Instance != null && _skillDefinition != null && _caster != null && button.interactable) // Use the singleton instance
        {
            BattleUI.Instance.ShowSkillTooltip(_skillDefinition, _caster);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (BattleUI.Instance != null) // Use the singleton instance
        {
            BattleUI.Instance.HideSkillTooltip();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (BattleUI.Instance != null && _skillDefinition != null && _caster != null && button.interactable) // Use the singleton instance
        {
            BattleUI.Instance.ShowSkillTooltip(_skillDefinition, _caster);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (BattleUI.Instance != null) // Use the singleton instance
        {
            BattleUI.Instance.HideSkillTooltip();
        }
    }
}