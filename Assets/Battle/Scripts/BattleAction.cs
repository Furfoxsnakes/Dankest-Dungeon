using DankestDungeon.Skills; // For SkillDefinitionSO

public class BattleAction
{
    public Character Actor { get; }
    public Character Target { get; }
    public ActionType ActionType { get; }
    public SkillDefinitionSO UsedSkill { get; } // Standardized name

    // Constructor for skill-based actions
    public BattleAction(Character actor, Character target, SkillDefinitionSO usedSkill)
    {
        Actor = actor;
        Target = target; // Target can be null if not yet selected or for AoE/Self
        ActionType = ActionType.Skill;
        UsedSkill = usedSkill;
    }

    // Constructor for non-skill actions (like basic attack, defend, item, skip)
    public BattleAction(Character actor, Character target, ActionType actionType)
    {
        Actor = actor;
        Target = target;
        ActionType = actionType;
        UsedSkill = null; // No skill for these types
    }
}