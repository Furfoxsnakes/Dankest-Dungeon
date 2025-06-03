using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Reference to the battle UI
    public BattleUI battleUI;

    // Reference to the inventory UI
    public InventoryUI inventoryUI;

    void Start()
    {
        // Initialize UI elements
        InitializeUI();
    }

    void InitializeUI()
    {
        // Set up the initial state of the UI
        battleUI.gameObject.SetActive(false);
        inventoryUI.gameObject.SetActive(false);
    }

    public void ShowBattleUI()
    {
        battleUI.gameObject.SetActive(true);
        inventoryUI.gameObject.SetActive(false);
    }

    public void ShowInventoryUI()
    {
        battleUI.gameObject.SetActive(false);
        inventoryUI.gameObject.SetActive(true);
    }
}