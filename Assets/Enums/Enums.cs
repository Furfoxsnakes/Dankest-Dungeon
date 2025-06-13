public enum BattleEvent
{
    None,
    SetupComplete,
    PlayerSkillSelected,
    TargetSelected,
    TargetSelectionCancelled,
    EnemyActionComplete,
    ActionExecutionFinished, // New event
    ActionFullyComplete,
    VictoryProcessed,
    DefeatProcessed
    // ... other existing events
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
    Health,
    MaxHealth,
    Mana,       // Current mana, if you want to modify it directly
    MaxMana,    // Add this for maximum mana
    AttackPower,
    Defense,
    MagicPower,
    MagicResistance,
    Speed,
    CritChance,
    Accuracy,
    Dodge,
    None
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

// Add this enum
public enum DamageNumberType
{
    NormalDamage,
    CriticalDamage,
    Heal,
    CriticalHeal,
    StatusEffectDamage,
    StatusEffectHeal,
    Miss,
    Dodge,
    Block
}