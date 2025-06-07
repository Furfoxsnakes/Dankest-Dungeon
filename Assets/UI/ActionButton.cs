using UnityEngine;
using UnityEngine.UI;
using TMPro; // If you use TextMeshPro for skill names
using DankestDungeon.Skills;
using System; // For Action

public class ActionButton : MonoBehaviour
{
    public Button button;
    public Image iconImage;
    public TextMeshProUGUI nameText; // Optional: Assign if you want to display skill name

    private SkillDefinitionSO _skillDefinition;
    private Action<SkillDefinitionSO> _onClickAction;

    void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        // Ensure iconImage and nameText are assigned in the prefab or found here
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

    public void Setup(SkillDefinitionSO skillDef, Action<SkillDefinitionSO> onClickCallback)
    {
        _skillDefinition = skillDef;
        _onClickAction = onClickCallback;

        if (skillDef == null)
        {
            Debug.LogError("SkillDefinitionSO is null in ActionButtonUI.Setup", this);
            if(iconImage != null) iconImage.enabled = false;
            if(nameText != null) nameText.text = "Error";
            return;
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
        else if (nameText != null)
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
    }
}