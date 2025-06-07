using DankestDungeon.Skills; // Your skills namespace
using UnityEngine;

public class BattleAction
{
    public Character Actor { get; private set; }
    public Character Target { get; private set; }
    public ActionType ActionType { get; private set; }
    public SkillDefinitionSO UsedSkill { get; private set; } // The skill being performed
    public int SkillRank { get; private set; } // The rank of the skill being used

    // Constructor for non-skill actions (if you still have them)
    public BattleAction(Character actor, Character target, ActionType actionType)
    {
        Actor = actor;
        Target = target;
        ActionType = actionType;
        UsedSkill = null;
        SkillRank = 0;
    }

    // Constructor for skill-based actions, target might be known or TBD
    public BattleAction(Character actor, Character target, SkillDefinitionSO skill, int rank)
    {
        Actor = actor;
        Target = target;
        ActionType = ActionType.Skill;
        UsedSkill = skill;
        SkillRank = rank;
    }
    
    // Constructor for skill-based actions when target selection is needed
    public BattleAction(Character actor, SkillDefinitionSO skill, int rank)
    {
        Actor = actor;
        Target = null; // Target will be set by TargetSelectionState
        ActionType = ActionType.Skill;
        UsedSkill = skill;
        SkillRank = rank;
    }

    public void SetTarget(Character target)
    {
        Target = target;
    }
}