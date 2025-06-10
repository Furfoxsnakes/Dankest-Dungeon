using UnityEngine;
using DankestDungeon.Skills; // For SkillDefinitionSO, SkillTargetType
using DankestDungeon.Characters; // For Character
using System.Collections.Generic;
using System.Linq; // For Linq operations like Where, ToList

public class TargetingSystem : MonoBehaviour // Or 'public class TargetingSystem' if not a MonoBehaviour
{
    private List<Character> _playerTeamCharacters;
    private List<Character> _enemyTeamCharacters;

    // Call this from CombatSystem when teams are set up
    public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
    {
        _playerTeamCharacters = playerTeam;
        _enemyTeamCharacters = enemyTeam;
    }

    // Add this method
    public List<Character> GetValidTargets(Character caster, SkillDefinitionSO skill)
    {
        if (skill == null || caster == null)
        {
            Debug.LogError("[TargetingSystem] GetValidTargets called with null skill or caster.");
            return new List<Character>();
        }

        List<Character> validTargets = new List<Character>();

        // Determine which team is friendly and which is hostile relative to the caster
        bool isCasterPlayer = _playerTeamCharacters.Contains(caster);
        List<Character> friendlyTeam = isCasterPlayer ? _playerTeamCharacters : _enemyTeamCharacters;
        List<Character> hostileTeam = isCasterPlayer ? _enemyTeamCharacters : _playerTeamCharacters;

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                if (caster.IsAlive)
                {
                    validTargets.Add(caster);
                }
                break;

            case SkillTargetType.SingleAlly:
                validTargets.AddRange(friendlyTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.SingleEnemy:
                validTargets.AddRange(hostileTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.AllAllies:
                // For "All" types, TargetSelectionState might not even be entered.
                // But if it is, or if this method is used elsewhere, it should return all valid allies.
                validTargets.AddRange(friendlyTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.AllEnemies:
                validTargets.AddRange(hostileTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.AllyRow:
                // Placeholder: Implement row-based targeting.
                // You'll need a way to know which row characters are in.
                // This might involve checking their position in the formationManager.
                Debug.LogWarning("AllyRow targeting not fully implemented in GetValidTargets.");
                validTargets.AddRange(friendlyTeam.Where(c => c.IsAlive)); // Fallback to all allies for now
                break;

            case SkillTargetType.EnemyRow:
                // Placeholder: Implement row-based targeting.
                Debug.LogWarning("EnemyRow targeting not fully implemented in GetValidTargets.");
                validTargets.AddRange(hostileTeam.Where(c => c.IsAlive)); // Fallback to all enemies for now
                break;
            
            case SkillTargetType.None: // Skills that don't target anyone specifically (e.g. some summons)
                // No specific targets to select
                break;

            default:
                Debug.LogWarning($"[TargetingSystem] Unhandled SkillTargetType: {skill.targetType} for skill {skill.skillNameKey}");
                break;
        }

        // Further filtering can be done here, e.g., for skills that can only target characters with specific status effects,
        // or based on range if you implement that.

        return validTargets;
    }

    // You might also have a GetTargetsForSkill method used by ActionSequenceHandler
    // for AoE skills that don't go through manual selection.
    public List<Character> GetTargetsForSkill(Character caster, SkillDefinitionSO skillDef, List<Character> allPlayers, List<Character> allEnemies)
    {
        if (skillDef == null || caster == null) return new List<Character>();

        List<Character> resolvedTargets = new List<Character>();
        bool isCasterPlayer = allPlayers.Contains(caster);
        List<Character> friendlyTeam = isCasterPlayer ? allPlayers : allEnemies;
        List<Character> hostileTeam = isCasterPlayer ? allEnemies : allPlayers;

        switch (skillDef.targetType)
        {
            case SkillTargetType.Self:
                if (caster.IsAlive) resolvedTargets.Add(caster);
                break;
            case SkillTargetType.AllAllies:
                resolvedTargets.AddRange(friendlyTeam.Where(c => c.IsAlive));
                break;
            case SkillTargetType.AllEnemies:
                resolvedTargets.AddRange(hostileTeam.Where(c => c.IsAlive));
                break;
            case SkillTargetType.SingleAlly:
            case SkillTargetType.SingleEnemy:
                // For single-target skills, ActionSequenceHandler should use action.Target.
                // This method is primarily for resolving targets for AoE, Self, etc.
                // Returning an empty list here is expected for these cases in this method.
                break; // No warning, just break and return empty resolvedTargets.
             // Add other cases like AllyRow, EnemyRow if they are resolved here for AoE application
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

        bool isCasterPlayer = _playerTeamCharacters.Contains(caster);
        List<Character> friendlyTeam = isCasterPlayer ? _playerTeamCharacters : _enemyTeamCharacters;
        List<Character> hostileTeam = isCasterPlayer ? _enemyTeamCharacters : _playerTeamCharacters;

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                if (caster.IsAlive)
                {
                    finalTargets.Add(caster);
                }
                break;

            case SkillTargetType.SingleAlly:
                if (primaryTarget != null && primaryTarget.IsAlive && friendlyTeam.Contains(primaryTarget))
                {
                    finalTargets.Add(primaryTarget);
                }
                else if (primaryTarget != null)
                {
                    Debug.LogWarning($"[TargetingSystem] Invalid primary target {primaryTarget.GetName()} for SingleAlly skill {skill.skillNameKey} or target not in friendly team.");
                }
                else
                {
                     Debug.LogWarning($"[TargetingSystem] Null primary target for SingleAlly skill {skill.skillNameKey}.");
                }
                break;

            case SkillTargetType.SingleEnemy:
                if (primaryTarget != null && primaryTarget.IsAlive && hostileTeam.Contains(primaryTarget))
                {
                    finalTargets.Add(primaryTarget);
                }
                else if (primaryTarget != null)
                {
                    Debug.LogWarning($"[TargetingSystem] Invalid primary target {primaryTarget.GetName()} for SingleEnemy skill {skill.skillNameKey} or target not in hostile team.");
                }
                else
                {
                    Debug.LogWarning($"[TargetingSystem] Null primary target for SingleEnemy skill {skill.skillNameKey}.");
                }
                break;

            case SkillTargetType.AllAllies:
                finalTargets.AddRange(friendlyTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.AllEnemies:
                finalTargets.AddRange(hostileTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.AllyRow:
                // Placeholder: Implement actual row targeting logic.
                // This might involve using the primaryTarget to identify a row, or specific formation data.
                Debug.LogWarning($"[TargetingSystem] AllyRow targeting in DetermineFinalTargets is not fully implemented. Defaulting to all living allies.");
                finalTargets.AddRange(friendlyTeam.Where(c => c.IsAlive));
                break;

            case SkillTargetType.EnemyRow:
                // Placeholder: Implement actual row targeting logic.
                Debug.LogWarning($"[TargetingSystem] EnemyRow targeting in DetermineFinalTargets is not fully implemented. Defaulting to all living enemies.");
                finalTargets.AddRange(hostileTeam.Where(c => c.IsAlive));
                break;
            
            case SkillTargetType.None:
                // Skills that don't target anyone specifically.
                break;

            default:
                Debug.LogWarning($"[TargetingSystem] Unhandled SkillTargetType in DetermineFinalTargets: {skill.targetType} for skill {skill.skillNameKey}");
                break;
        }

        return finalTargets;
    }
}