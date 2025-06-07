using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like ToList() and Any()
using DankestDungeon.Skills; // Required for SkillDefinitionSO and SkillTargetType

[CreateAssetMenu(fileName = "AggressiveAI", menuName = "Dankest Dungeon/AI/Aggressive")]
public class AggressiveAI : AIBehaviorSO
{
    [Tooltip("Chance to attempt using a skill instead of a basic attack (0-100)")]
    [Range(0, 100)]
    [SerializeField] private int skillChance = 75; // Increased chance to see skills in action

    // The old magicChance and spells array are less relevant if skills handle magical attacks.
    // We can remove them or adapt them if you have generic "Magic" actions separate from skills.
    // For now, let's focus on the skill system.

    public override BattleAction DecideAction(Character self, List<Character> allies, List<Character> enemies)
    {
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();
        if (aliveEnemies.Count == 0 && !(self.LearnedSkills.Keys.Any(s => s.targetType == SkillTargetType.Self || s.targetType == SkillTargetType.SingleAlly || s.targetType == SkillTargetType.AllAllies)))
        {
            Debug.LogWarning($"[AI] {self.GetName()}: No alive enemies and no self/ally skills to use.");
            return null; // No action if no enemies and no self/ally skills
        }

        // Attempt to use a skill
        if (Random.Range(0, 100) < skillChance)
        {
            var usableSkills = self.LearnedSkills.Keys
                .Where(skill => CanUseSkill(self, skill, allies, aliveEnemies))
                .ToList();

            if (usableSkills.Any())
            {
                SkillDefinitionSO chosenSkill = usableSkills[Random.Range(0, usableSkills.Count)];
                Character primaryTarget = DetermineTargetForSkill(self, chosenSkill, allies, aliveEnemies);
                int skillRank = self.GetSkillRank(chosenSkill);

                // If the skill requires a target but none could be found, fall back.
                if (IsTargetRequired(chosenSkill.targetType) && primaryTarget == null)
                {
                     Debug.LogWarning($"[AI] {self.GetName()}: Skill {chosenSkill.skillNameKey} requires a target, but none found. Falling back to basic attack.");
                     return FallbackToBasicAttack(self, aliveEnemies);
                }
                
                Debug.Log($"[AI] {self.GetName()} decided to use SKILL: {chosenSkill.skillNameKey} on {primaryTarget?.GetName() ?? "area/self"}");
                return new BattleAction(self, primaryTarget, chosenSkill, skillRank);
            }
            else
            {
                Debug.Log($"[AI] {self.GetName()}: Wanted to use a skill, but no usable skills found. Falling back.");
            }
        }

        // Fallback to basic attack if no skill is chosen or usable
        return FallbackToBasicAttack(self, aliveEnemies);
    }

    private BattleAction FallbackToBasicAttack(Character self, List<Character> aliveEnemies)
    {
        if (aliveEnemies.Count == 0)
        {
            Debug.Log($"[AI] {self.GetName()}: Fallback to basic attack, but no alive enemies.");
            return null; // Cannot attack if no one is alive
        }
        Character target = FindLowestHealthTarget(aliveEnemies); // Or random, or other logic
        Debug.Log($"[AI] {self.GetName()} decided to use basic ATTACK on {target.GetName()}");
        return new BattleAction(self, target, ActionType.Attack);
    }

    private bool CanUseSkill(Character self, SkillDefinitionSO skill, List<Character> allies, List<Character> enemies)
    {
        // Basic check: Does the skill have a valid target type?
        // More complex checks could be added here (e.g., cooldowns, resource costs, situational logic)
        switch (skill.targetType)
        {
            case SkillTargetType.None:
            case SkillTargetType.Self:
                return true; // Always usable if self or no target
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.AllEnemies:
                return enemies.Any(e => e != null && e.IsAlive);
            case SkillTargetType.SingleAlly:
            case SkillTargetType.AllAllies:
                return allies.Any(a => a != null && a.IsAlive);
            // Add cases for other target types if you have them (e.g., DeadAlly for revive)
            default:
                return false;
        }
    }
    
    private bool IsTargetRequired(SkillTargetType targetType)
    {
        switch (targetType)
        {
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.SingleAlly:
            // case SkillTargetType.SingleDeadAlly: // If you have revive skills
                return true;
            case SkillTargetType.None:
            case SkillTargetType.Self:
            case SkillTargetType.AllEnemies: // Primary target might be optional for animation
            case SkillTargetType.AllAllies:  // Primary target might be optional for animation
                return false; 
            default:
                return true; // Assume target required for unhandled types
        }
    }

    private Character DetermineTargetForSkill(Character self, SkillDefinitionSO skill, List<Character> allies, List<Character> enemies)
    {
        var aliveAllies = allies.Where(a => a != null && a.IsAlive).ToList();
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                return self;
            case SkillTargetType.SingleEnemy:
                return aliveEnemies.Any() ? FindLowestHealthTarget(aliveEnemies) : null; // Example: target lowest health
            case SkillTargetType.AllEnemies:
                return aliveEnemies.Any() ? aliveEnemies.First() : null; // Primary target for animation, CombatSystem handles all
            case SkillTargetType.SingleAlly:
                // Example: Target lowest health ally for a heal, or self if it's a buff and no other allies
                var alliesToConsider = aliveAllies.Any() ? aliveAllies : new List<Character> { self };
                return FindLowestHealthTarget(alliesToConsider.Where(a => a.CurrentHealth < a.Stats.maxHealth).ToList(), self); // Prioritize injured, fallback to self
            case SkillTargetType.AllAllies:
                return aliveAllies.Any() ? aliveAllies.First() : self; // Primary target for animation
            case SkillTargetType.None:
                return null;
            // Add other cases as needed (e.g., specific targeting logic for buffs, debuffs)
            default:
                Debug.LogWarning($"[AI] Unhandled skill target type: {skill.targetType} for skill {skill.skillNameKey}. Returning null target.");
                return null;
        }
    }

    private Character FindLowestHealthTarget(List<Character> targets, Character defaultTarget = null)
    {
        if (targets == null || !targets.Any()) return defaultTarget;

        Character lowestHealthTarget = targets[0];
        foreach (var target in targets)
        {
            if (target.CurrentHealth < lowestHealthTarget.CurrentHealth)
            {
                lowestHealthTarget = target;
            }
        }
        return lowestHealthTarget;
    }
}