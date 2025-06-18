using UnityEngine;
using System.Collections.Generic;
using System;
using DankestDungeon.Skills; // For EquipmentSlot, StatType
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

public class CharacterEquipment : MonoBehaviour
{
    [BoxGroup("Assigned Equipment In Editor", CenterLabel = true)]
    [DetailedInfoBox("Assign equippable items here for initial setup. Runtime changes will modify the character's equipment in play, but won't automatically update these Inspector fields.", "", InfoMessageType.Info)]

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Weapon", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match MainHand slot!")]
    [SerializeField] private ItemSO _mainHandItem;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Off-Hand", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match OffHand slot!")]
    [SerializeField] private ItemSO _offHandItem;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Head", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match Head slot!")]
    [SerializeField] private ItemSO _headItem;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Chest", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match Chest slot!")]
    [SerializeField] private ItemSO _chestItem;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Legs", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match Legs slot!")]
    [SerializeField] private ItemSO _legsItem;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Feet", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match Feet slot!")]
    [SerializeField] private ItemSO _feetItem;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Accessory 1", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match Accessory1 slot!")]
    [SerializeField] private ItemSO _accessory1Item;

    [BoxGroup("Assigned Equipment In Editor/Slots")]
    [Title("Accessory 2", bold:false), AssetsOnly, ValidateInput("ValidateItemForSlot", "Item's EquipSlot type does not match Accessory2 slot!")]
    [SerializeField] private ItemSO _accessory2Item;

    // Renamed from _runtimeEquippedItems
    private Dictionary<EquipmentSlot, ItemSO> _equippedItems = new Dictionary<EquipmentSlot, ItemSO>();
    public IReadOnlyDictionary<EquipmentSlot, ItemSO> GetEquippedItems() => _equippedItems;


    public event Action OnEquipmentChanged;
    private Character character;

    void Awake()
    {
        character = GetComponentInParent<Character>();
        if (character == null)
        {
            Debug.LogError($"CharacterEquipment on {gameObject.name} could not find a Character component in its parents. Ensure it's part of a Character hierarchy.");
        }
        InitializeEquipmentFromInspector();
    }

    private void InitializeEquipmentFromInspector()
    {
        _equippedItems.Clear();

        // Attempt to equip items assigned in the Inspector
        TryEquipInitialItem(EquipmentSlot.MainHand, _mainHandItem);
        TryEquipInitialItem(EquipmentSlot.OffHand, _offHandItem);
        TryEquipInitialItem(EquipmentSlot.Head, _headItem);
        TryEquipInitialItem(EquipmentSlot.Chest, _chestItem);
        TryEquipInitialItem(EquipmentSlot.Legs, _legsItem);
        TryEquipInitialItem(EquipmentSlot.Feet, _feetItem);
        TryEquipInitialItem(EquipmentSlot.Accessory1, _accessory1Item);
        TryEquipInitialItem(EquipmentSlot.Accessory2, _accessory2Item);

        // Ensure all slots are present in the runtime dictionary, even if null
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (!_equippedItems.ContainsKey(slot))
            {
                _equippedItems.Add(slot, null);
            }
        }
    }

    private void TryEquipInitialItem(EquipmentSlot slot, ItemSO item)
    {
        if (item != null)
        {
            if (item.equipSlot == slot)
            {
                _equippedItems[slot] = item;
                // Debug.Log($"{character?.GetName() ?? gameObject.name} initialized with {item.itemName} in {slot}.");
            }
            else
            {
                Debug.LogWarning($"Item '{item.itemName}' assigned to '{slot}' in Inspector on {character?.GetName() ?? gameObject.name} has an incorrect internal EquipSlot type ('{item.equipSlot}'). It will NOT be equipped in '{slot}'. Please correct the item assignment or the ItemSO's EquipSlot property.");
            }
        }
    }

    public ItemSO GetItemInSlot(EquipmentSlot slot)
    {
        _equippedItems.TryGetValue(slot, out ItemSO item);
        return item;
    }

    public void EquipItem(ItemSO itemToEquip)
    {
        if (itemToEquip == null) return;

        EquipmentSlot targetSlot = itemToEquip.equipSlot;

        if (_equippedItems.ContainsKey(targetSlot) && _equippedItems[targetSlot] != null)
        {
            UnequipItem(targetSlot); 
        }

        _equippedItems[targetSlot] = itemToEquip;
        Debug.Log($"{character?.GetName() ?? gameObject.name} equipped {itemToEquip.itemName} in {targetSlot}.");
        OnEquipmentChanged?.Invoke();
    }

    public ItemSO UnequipItem(EquipmentSlot slot)
    {
        if (_equippedItems.ContainsKey(slot) && _equippedItems[slot] != null)
        {
            ItemSO unequippedItem = _equippedItems[slot];
            _equippedItems[slot] = null;
            Debug.Log($"{character?.GetName() ?? gameObject.name} unequipped {unequippedItem.itemName} from {slot}.");
            OnEquipmentChanged?.Invoke();
            return unequippedItem;
        }
        return null;
    }

    public float GetBonusForStat(StatType statType)
    {
        float bonus = 0f;
        foreach (ItemSO item in _equippedItems.Values)
        {
            if (item != null)
            {
                foreach (StatModifier modifier in item.modifiers)
                {
                    if (modifier.Stat == statType)
                    {
                        bonus += modifier.Value;
                    }
                }
            }
        }
        return bonus;
    }

#if UNITY_EDITOR
    private bool ValidateItemForSlot(ItemSO itemValue, InspectorProperty property)
    {
        if (itemValue == null) return true; 

        EquipmentSlot expectedSlot;
        switch (property.Name) 
        {
            case nameof(_mainHandItem): expectedSlot = EquipmentSlot.MainHand; break;
            case nameof(_offHandItem):  expectedSlot = EquipmentSlot.OffHand;  break;
            case nameof(_headItem):     expectedSlot = EquipmentSlot.Head;     break;
            case nameof(_chestItem):    expectedSlot = EquipmentSlot.Chest;    break;
            case nameof(_legsItem):     expectedSlot = EquipmentSlot.Legs;     break;
            case nameof(_feetItem):     expectedSlot = EquipmentSlot.Feet;     break;
            case nameof(_accessory1Item): expectedSlot = EquipmentSlot.Accessory1; break;
            case nameof(_accessory2Item): expectedSlot = EquipmentSlot.Accessory2; break;
            default:
                Debug.LogWarning($"[CharacterEquipment] ValidateItemForSlot called for an unhandled property: '{property.Name}' (Path: '{property.Path}'). Cannot determine expected slot. Validation will pass by default for this property to avoid UI lock.");
                return true; 
        }
        
        bool isValid = itemValue.equipSlot == expectedSlot;
        return isValid; 
    }
#endif
}