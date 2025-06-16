using UnityEngine;
using DankestDungeon.Skills; // For SkillDefinitionSO, SkillTargetType
using DankestDungeon.Characters; // For Character
using System.Collections.Generic;
using System.Linq; // For Linq operations like Where, ToList

public class TargetingSystem : MonoBehaviour
{
    private List<Character> _playerTeamCharacters;
    private List<Character> _enemyTeamCharacters;

    public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
    {
        _playerTeamCharacters = playerTeam;
        _enemyTeamCharacters = enemyTeam;
    }

    public List<Character> GetValidTargets(Character caster, SkillDefinitionSO skill)
    {
        if (skill == null || caster == null)
        {
            Debug.LogError("[TargetingSystem] GetValidTargets called with null skill or caster.");
            return new List<Character>();
        }

        // Character.FormationPosition is the 0-indexed logical rank.
        // skill.launchPositions and skill.targetPositions are 0-indexed logical ranks.
        if (caster.FormationPosition < 0 || !skill.launchPositions.Contains(caster.FormationPosition))
        {
            // Debug.Log($"[TargetingSystem] Caster {caster.GetName()} at logical rank {caster.FormationPosition} cannot use skill {skill.skillNameKey} from this rank. Valid launch ranks: [{string.Join(",", skill.launchPositions)}]");
            return new List<Character>(); 
        }

        if (skill.targetPositions == null || skill.targetPositions.Count == 0)
        {
            if (skill.targetType != SkillTargetType.Self && skill.targetType != SkillTargetType.None)
            {
                Debug.LogWarning($"[TargetingSystem] Skill {skill.skillNameKey} has no target ranks defined, but requires targets.");
                return new List<Character>();
            }
        }
        
        List<Character> validTargets = new List<Character>();
        bool isCasterPlayer = _playerTeamCharacters.Contains(caster);
        List<Character> friendlyTeam = isCasterPlayer ? _playerTeamCharacters : _enemyTeamCharacters;
        List<Character> hostileTeam = isCasterPlayer ? _enemyTeamCharacters : _playerTeamCharacters;

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                if (caster.IsAlive) validTargets.Add(caster);
                break;
            case SkillTargetType.SingleAlly:
            case SkillTargetType.AllAllies:
                validTargets.AddRange(friendlyTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.AllEnemies:
                validTargets.AddRange(hostileTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.AllyRow:
                Debug.LogWarning("AllyRow targeting in GetValidTargets: using targetPositions, full row logic (based on primary target's row) pending.");
                validTargets.AddRange(friendlyTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.EnemyRow:
                Debug.LogWarning("EnemyRow targeting in GetValidTargets: using targetPositions, full row logic (based on primary target's row) pending.");
                validTargets.AddRange(hostileTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.None:
                break;
            default:
                Debug.LogWarning($"[TargetingSystem] Unhandled SkillTargetType: {skill.targetType} for skill {skill.skillNameKey}");
                break;
        }
        return validTargets;
    }

    public List<Character> GetTargetsForSkill(Character caster, SkillDefinitionSO skillDef, List<Character> allPlayers, List<Character> allEnemies)
    {
        if (skillDef == null || caster == null) return new List<Character>();

        List<Character> resolvedTargets = new List<Character>();
        bool isCasterPlayer = allPlayers.Contains(caster);
        List<Character> friendlyTeam = isCasterPlayer ? allPlayers : allEnemies;
        List<Character> hostileTeam = isCasterPlayer ? allEnemies : allPlayers;

        if (skillDef.targetType != SkillTargetType.Self && skillDef.targetType != SkillTargetType.None &&
            (skillDef.targetPositions == null || skillDef.targetPositions.Count == 0))
        {
            Debug.LogWarning($"[TargetingSystem] GetTargetsForSkill: Skill {skillDef.skillNameKey} has no target ranks defined.");
        }

        switch (skillDef.targetType)
        {
            case SkillTargetType.Self:
                if (caster.IsAlive) resolvedTargets.Add(caster);
                break;
            case SkillTargetType.AllAllies:
                resolvedTargets.AddRange(friendlyTeam.Where(c => c.IsAlive && skillDef.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.AllEnemies:
                resolvedTargets.AddRange(hostileTeam.Where(c => c.IsAlive && skillDef.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.SingleAlly:
            case SkillTargetType.SingleEnemy:
                break; 
            case SkillTargetType.AllyRow:
                 Debug.LogWarning($"[TargetingSystem] GetTargetsForSkill: AllyRow targeting needs full implementation including target ranks.");
                 resolvedTargets.AddRange(friendlyTeam.Where(c => c.IsAlive && skillDef.targetPositions.Contains(c.FormationPosition))); 
                 break;
            case SkillTargetType.EnemyRow:
                 Debug.LogWarning($"[TargetingSystem] GetTargetsForSkill: EnemyRow targeting needs full implementation including target ranks.");
                 resolvedTargets.AddRange(hostileTeam.Where(c => c.IsAlive && skillDef.targetPositions.Contains(c.FormationPosition)));
                 break;
            default:
                Debug.LogWarning($"[TargetingSystem] GetTargetsForSkill: Unhandled or inappropriate SkillTargetType: {skillDef.targetType}");
                break;
        }
        return resolvedTargets;
    }

    public List<Character> DetermineFinalTargets(Character caster, Character primaryTarget, SkillDefinitionSO skill)
    {
        List<Character> finalTargets = new List<Character>();
        if (skill == null || caster == null)
        {
            Debug.LogError("[TargetingSystem] DetermineFinalTargets called with null skill or caster.");
            return finalTargets;
        }
        if (_playerTeamCharacters == null || _enemyTeamCharacters == null)
        {
            Debug.LogError("[TargetingSystem] Teams not initialized. Call InitializeTeams first.");
            return finalTargets;
        }

        if (caster.FormationPosition < 0 || !skill.launchPositions.Contains(caster.FormationPosition))
        {
            // Debug.LogWarning($"[TargetingSystem] DetermineFinalTargets: Caster {caster.GetName()} at logical rank {caster.FormationPosition} cannot use skill {skill.skillNameKey} from this rank.");
            return finalTargets; 
        }
        if (skill.targetType != SkillTargetType.Self && skill.targetType != SkillTargetType.None &&
            (skill.targetPositions == null || skill.targetPositions.Count == 0))
        {
            Debug.LogWarning($"[TargetingSystem] DetermineFinalTargets: Skill {skill.skillNameKey} has no target ranks defined.");
        }

        bool isCasterPlayer = _playerTeamCharacters.Contains(caster);
        List<Character> friendlyTeam = isCasterPlayer ? _playerTeamCharacters : _enemyTeamCharacters;
        List<Character> hostileTeam = isCasterPlayer ? _enemyTeamCharacters : _playerTeamCharacters;

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                if (caster.IsAlive) finalTargets.Add(caster);
                break;
            case SkillTargetType.SingleAlly:
                if (primaryTarget != null && primaryTarget.IsAlive && friendlyTeam.Contains(primaryTarget) &&
                    skill.targetPositions.Contains(primaryTarget.FormationPosition))
                {
                    finalTargets.Add(primaryTarget);
                }
                else if (primaryTarget != null)
                {
                    string reason = "";
                    if (!primaryTarget.IsAlive) reason = "not alive";
                    else if (!friendlyTeam.Contains(primaryTarget)) reason = "not in friendly team";
                    else if (primaryTarget.FormationPosition < 0 || !skill.targetPositions.Contains(primaryTarget.FormationPosition)) 
                        reason = $"not in a valid target rank (rank {primaryTarget.FormationPosition}, valid ranks: [{string.Join(",", skill.targetPositions)}])";
                    else reason = "unknown";
                    Debug.LogWarning($"[TargetingSystem] Invalid primary target {primaryTarget.GetName()} for SingleAlly skill {skill.skillNameKey}. Reason: {reason}.");
                }
                else Debug.LogWarning($"[TargetingSystem] Null primary target for SingleAlly skill {skill.skillNameKey}.");
                break;
            case SkillTargetType.SingleEnemy:
                if (primaryTarget != null && primaryTarget.IsAlive && hostileTeam.Contains(primaryTarget) &&
                    skill.targetPositions.Contains(primaryTarget.FormationPosition))
                {
                    finalTargets.Add(primaryTarget);
                }
                else if (primaryTarget != null)
                {
                    string reason = "";
                    if (!primaryTarget.IsAlive) reason = "not alive";
                    else if (!hostileTeam.Contains(primaryTarget)) reason = "not in hostile team";
                    else if (primaryTarget.FormationPosition < 0 || !skill.targetPositions.Contains(primaryTarget.FormationPosition)) 
                        reason = $"not in a valid target rank (rank {primaryTarget.FormationPosition}, valid ranks: [{string.Join(",", skill.targetPositions)}])";
                    else reason = "unknown";
                    Debug.LogWarning($"[TargetingSystem] Invalid primary target {primaryTarget.GetName()} for SingleEnemy skill {skill.skillNameKey}. Reason: {reason}.");
                }
                else Debug.LogWarning($"[TargetingSystem] Null primary target for SingleEnemy skill {skill.skillNameKey}.");
                break;
            case SkillTargetType.AllAllies:
                finalTargets.AddRange(friendlyTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.AllEnemies:
                finalTargets.AddRange(hostileTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.AllyRow:
                Debug.LogWarning($"[TargetingSystem] AllyRow targeting in DetermineFinalTargets not fully implemented. Defaulting to living allies in valid target ranks.");
                finalTargets.AddRange(friendlyTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.EnemyRow:
                Debug.LogWarning($"[TargetingSystem] EnemyRow targeting in DetermineFinalTargets not fully implemented. Defaulting to living enemies in valid target ranks.");
                finalTargets.AddRange(hostileTeam.Where(c => c.IsAlive && skill.targetPositions.Contains(c.FormationPosition)));
                break;
            case SkillTargetType.None:
                break;
            default:
                Debug.LogWarning($"[TargetingSystem] Unhandled SkillTargetType in DetermineFinalTargets: {skill.targetType} for skill {skill.skillNameKey}");
                break;
        }
        return finalTargets;
    }
}