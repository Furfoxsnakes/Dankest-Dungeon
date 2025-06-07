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
    MoveTarget, // For push/pull mechanics
    // Add more as needed
}

public enum StatType // Example stats, expand as needed
{
    None, // For effects that don't target a specific stat (e.g. ApplyStatusEffect)
    MaxHealth,
    CurrentHealth, // Usually for direct healing/damage, not buffs
    AttackPower,
    Defense,
    Speed,
    Accuracy,
    Dodge,
    CritChance,
    MagicPower,
    MagicResistance
    // Add more RPG stats
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