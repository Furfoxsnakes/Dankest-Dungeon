using DankestDungeon.Skills;
using System.Collections.Generic; // Required for List

public class BattleAction
{
    public Character Actor { get; }
    public Character PrimaryTarget { get; } // The initially selected target, can be null
    public ActionType ActionType { get; }
    public SkillDefinitionSO UsedSkill { get; }
    public SkillRankData SkillRank { get; }
    
    /// <summary>
    /// List of all characters that will be affected by this action.
    /// This list is typically populated by the targeting system after initial selection.
    /// </summary>
    public List<Character> ResolvedTargets { get; set; } 

    // Constructor for skill-based actions
    public BattleAction(Character actor, Character primaryTarget, SkillDefinitionSO usedSkill, SkillRankData skillRank)
    {
        Actor = actor;
        PrimaryTarget = primaryTarget; // This is the character initially selected or focused on
        ActionType = ActionType.Skill;
        UsedSkill = usedSkill;
        SkillRank = skillRank;
        ResolvedTargets = null; // Initialize as null; to be populated by targeting logic
    }

    // Constructor for non-skill actions (like basic attack, defend, item, skip)
    public BattleAction(Character actor, Character primaryTarget, ActionType actionType)
    {
        Actor = actor;
        PrimaryTarget = primaryTarget; // For single-target non-skill actions, this is the one affected
        ActionType = actionType;
        UsedSkill = null; 
        SkillRank = null; 
        ResolvedTargets = null; // Initialize as null; can be populated if non-skill actions might affect multiple targets
    }
}