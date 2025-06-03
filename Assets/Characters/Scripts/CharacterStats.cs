using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Dankest Dungeon/Character Stats")]
public class CharacterStats : ScriptableObject
{
    [Header("Character Info")]
    public string characterName;
    
    [Header("Base Stats")]
    public int maxHealth = 100;
    public int attackPower = 10;
    public int defense = 5;
    
    [Header("Magic Stats")]
    [SerializeField] public int magicPower = 10;
    [SerializeField] public int magicResistance = 5;
    
    [Header("Secondary Stats")]
    public int accuracy = 90; // Percentage, affects hit chance
    public int evasion = 5;   // Percentage, affects dodge chance
    public int speed = 10;    // Affects turn order
    
    [Header("Character Traits")]
    public CharacterClass characterClass;
    public CharacterRole role;
}



