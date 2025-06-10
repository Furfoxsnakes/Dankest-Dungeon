public enum BattleEvent
{
    None,
    SetupComplete,
    PlayerActionSelected,
    PlayerSkillSelected,  // New: When a skill is chosen from the UI
    TargetSelected,
    TargetSelectionCancelled,
    ActionFullyComplete,
    EnemyActionComplete,  // Added this event
    BattleWon,
    BattleLost,
    VictoryProcessed,
    DefeatProcessed
}

// If you don't already have these enums, add them:
public enum CharacterClass
{
    Warrior,
    Mage,
    Rogue,
    Cleric
}

public enum CharacterRole
{
    Tank,
    DamageDealer,
    Healer,
    Support
}

public enum CharacterEvent
{
    None,
    AttackComplete,
    HitComplete,
    DefendComplete,
    MagicComplete,
    ItemComplete, // Add this if not present
    HitKilled,
    DeathComplete
}

public enum ActionType
{
    Attack,
    Magic,
    Defend,
    Item,
    Flee,
    Skill,
    Skip
}

public enum SkillTargetType 
{ 
    None,
    Self, 
    SingleAlly, 
    AllAllies,
    AllyRow, // If you have row-based targeting
    SingleEnemy, 
    AllEnemies,
    EnemyRow, // If you have row-based targeting
    RandomEnemy,
    RandomAlly
}

public enum SkillEffectType
{
    Damage,
    Heal,
    BuffStat,
    DebuffStat,
    ApplyStatusEffect,
    ClearStatusEffect,
    MoveTarget,
    Revive // For push/pull mechanics
    // Add more as needed
}

public enum StatType
{
    None,
    Health,
    Mana,
    Stamina,
    AttackPower,
    Defense,
    MagicPower,
    MagicResistance,
    Speed,
    CritChance,
    MaxHealth,
    Accuracy,
    Dodge,
    // ... other stat types
}
public enum RowCategory
{
    Front,
    Back,
    Unknown
}

public enum AnimationType
{
    None,
    Attack,
    Hit,
    Cast,
    Defend,
    Item,
    Idle,
    Death,
    Victory,
    Flee,
}

public enum ElementType
{
    Physical, // Default or for non-elemental physical attacks
    Fire,
    Ice,
    Lightning,
    Poison,
    Holy,
    Shadow,
    Healing // Can also be an element if heals have types/resistances
    // Add more as needed
}

public enum AnimationTriggerName
{
    None, // Represents no trigger or an uninitialized state

    // Standard Character Actions
    Idle,
    Attack,
    Hit,
    Defend,
    Death,
    Move, // If you have a move trigger

    // Skill/Magic Related (can be generic or specific)
    Cast,          // Generic cast
    UseItem,       // Generic item use

    // --- Skill Specific Triggers ---
    // You can add specific skill triggers here if they are common
    // and you want to manage them via this enum.
    // Otherwise, SkillDefinitionSO can still hold a string for highly unique skill animations.
    // Example:
    // Fireball,
    // HealPulse,

    // --- UI/Other ---
    // Show,
    // Hide
}