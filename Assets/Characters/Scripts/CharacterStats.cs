using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Dankest Dungeon/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;
    
    [Header("Base Stats")]
    public int maxHealth = 100;
    public int maxMana = 50; // Add this line
    public int attackPower = 10;
    public int defense = 5;
    
    [Header("Magic Stats")]
    [SerializeField] public int magicPower = 10;
    [SerializeField] public int magicResistance = 5;
    
    [Header("Secondary Stats")]
    public int accuracy = 90; // Percentage, affects hit chance
    public int evasion = 5;   // Percentage, affects dodge chance
    public int speed = 10;    // Affects turn order
    public int criticalChance = 5; // Percentage, chance to deal critical damage
    public int criticalDamageMultiplier = 150; // Percentage, e.g., 150 means 1.5x damage on crit
    
    [Header("Character Traits")]
    public CharacterClass characterClass;
    public CharacterRole role;
}



