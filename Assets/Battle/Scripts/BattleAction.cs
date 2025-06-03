using UnityEngine;

public class BattleAction
{
    public Character Actor { get; private set; }
    public Character Target { get; private set; }
    public ActionType ActionType { get; private set; }
    public string SpellName { get; private set; }
    public int ItemId { get; private set; }
    
    public BattleAction(Character actor, Character target, ActionType actionType)
    {
        Actor = actor;
        Target = target;
        ActionType = actionType;
    }
    
    // Constructor for magic actions
    public BattleAction(Character actor, Character target, string spellName)
    {
        Actor = actor;
        Target = target;
        ActionType = ActionType.Magic;
        SpellName = spellName;
    }
    
    // Constructor for item actions
    public BattleAction(Character actor, Character target, int itemId)
    {
        Actor = actor;
        Target = target;
        ActionType = ActionType.Item;
        ItemId = itemId;
    }
}