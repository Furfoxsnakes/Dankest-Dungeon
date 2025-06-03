public enum BattleEvent
{
    None,
    SetupComplete,
    PlayerActionSelected,
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
    Skip
}