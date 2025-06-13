// filepath: d:\Development\Unity\Dankest Dungeon\Assets\Characters\Scripts\UI\CharacterUIBars.cs
using UnityEngine;
using UnityEngine.UI; // Required for Sliders

public class CharacterBattleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider; // Assuming you'll add a mana slider
	[SerializeField] private Character character;

    void LateUpdate()
    {
        if (character == null || !character.IsAlive)
        {
            // Optionally hide or disable the bars when the character is not alive
            if (healthSlider.gameObject.activeSelf) healthSlider.gameObject.SetActive(false);
            if (manaSlider.gameObject.activeSelf) manaSlider.gameObject.SetActive(false);
            return;
        }
        
        if (!healthSlider.gameObject.activeSelf) healthSlider.gameObject.SetActive(true);
        if (!manaSlider.gameObject.activeSelf) manaSlider.gameObject.SetActive(true);

        UpdateHealthBar();
        UpdateManaBar(); // You'll need to implement mana logic in Character and CharacterStats
    }

    public void UpdateHealthBar()
    {
        if (character != null && healthSlider != null && character.Stats != null)
        {
            float healthPercent = (float)character.CurrentHealth / character.GetMaxHealth();
            healthSlider.value = healthPercent;
        }
    }

    public void UpdateManaBar()
    {
        // TODO: Implement mana logic
        // Example:
        if (character != null && manaSlider != null && character.Stats != null)
        {
            float manaPercent = (float)character.CurrentMana / character.Stats.maxMana; // Assuming CurrentMana and maxMana exist
            manaSlider.value = manaPercent;
        }
        // if (manaSlider != null) manaSlider.gameObject.SetActive(false); // Hide if not implemented
    }
}