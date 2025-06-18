using UnityEngine;
using System.Collections.Generic;
using DankestDungeon.Skills; // For EquipmentSlot, StatType

[CreateAssetMenu(fileName = "NewItem", menuName = "Dankest Dungeon/Items/Equippable Item")]
public class ItemSO : ScriptableObject
{
    public string itemName = "New Item";
    [TextArea]
    public string itemDescription = "Item Description";
    public Sprite icon; // For UI
    public EquipmentSlot equipSlot; // The slot this item goes into

    public List<StatModifier> modifiers = new List<StatModifier>();

    // Future additions:
    // public int itemLevel;
    // public int goldValue;
    // public ItemRarity rarity;
    // public List<SkillEffectData> onEquipEffects; // e.g. grant a skill
}