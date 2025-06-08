using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills; // For SkillDefinitionSO, SkillTargetType
using System.Linq;
using Random = UnityEngine.Random;

public class TargetingSystem
{
	private List<Character> _playerTeamCharacters;
	private List<Character> _enemyTeamCharacters;

	public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
	{
		_playerTeamCharacters = playerTeam;
		_enemyTeamCharacters = enemyTeam;
	}

	public List<Character> DetermineFinalTargets(Character actor, Character primaryTarget, SkillDefinitionSO skill)
    {
        List<Character> finalTargets = new List<Character>();
        if (skill == null || actor == null)
        {
            Debug.LogError("[TARGETING] Skill or Actor is null.");
            return finalTargets;
        }

        if (_playerTeamCharacters == null || _enemyTeamCharacters == null)
        {
            Debug.LogError("[TARGETING] Teams not initialized in CombatSystem.");
            return finalTargets;
        }

        List<Character> actorAllies;
        List<Character> actorOpponents;

        // Determine actor's faction and define allies/opponents accordingly
        if (_playerTeamCharacters.Contains(actor))
        {
            actorAllies = _playerTeamCharacters.Where(c => c != null && c.IsAlive).ToList();
            actorOpponents = _enemyTeamCharacters.Where(c => c != null && c.IsAlive).ToList();
        }
        else if (_enemyTeamCharacters.Contains(actor))
        {
            actorAllies = _enemyTeamCharacters.Where(c => c != null && c.IsAlive).ToList();
            actorOpponents = _playerTeamCharacters.Where(c => c != null && c.IsAlive).ToList();
        }
        else
        {
            Debug.LogError($"[TARGETING] Actor {actor.GetName()} not found in any initialized team!");
            return finalTargets;
        }

        switch (skill.targetType)
        {
            case SkillTargetType.SingleEnemy:
                if (primaryTarget != null && primaryTarget.IsAlive && actorOpponents.Contains(primaryTarget))
                {
                    finalTargets.Add(primaryTarget);
                }
                else
                {
                    // Fallback: pick a random opponent if primary target is invalid
                    if (actorOpponents.Any())
                    {
                        Debug.LogWarning($"[TARGETING] SingleEnemy skill by {actor.GetName()} had invalid primary target. Targeting random enemy.");
                        finalTargets.Add(actorOpponents[Random.Range(0, actorOpponents.Count)]);
                    }
                    else
                    {
                        Debug.LogWarning($"[TARGETING] SingleEnemy skill by {actor.GetName()} has no valid targets.");
                    }
                }
                break;

            case SkillTargetType.AllEnemies:
                finalTargets.AddRange(actorOpponents);
                break;

            case SkillTargetType.SingleAlly:
                if (primaryTarget != null && primaryTarget.IsAlive && actorAllies.Contains(primaryTarget))
                {
                    finalTargets.Add(primaryTarget);
                }
                else
                {
                    // Fallback: pick a random ally if primary target is invalid
                    if (actorAllies.Any())
                    {
                         Debug.LogWarning($"[TARGETING] SingleAlly skill by {actor.GetName()} had invalid primary target. Targeting random ally.");
                        finalTargets.Add(actorAllies[Random.Range(0, actorAllies.Count)]);
                    }
                    else
                    {
                        Debug.LogWarning($"[TARGETING] SingleAlly skill by {actor.GetName()} has no valid targets.");
                    }
                }
                break;

            case SkillTargetType.AllAllies:
                finalTargets.AddRange(actorAllies);
                break;

            case SkillTargetType.Self:
                if (actor.IsAlive)
                {
                    finalTargets.Add(actor);
                }
                break;

            case SkillTargetType.None:
                // No specific targets needed, but skill might still affect areas or global state.
                break;

            default:
                Debug.LogError($"[TARGETING] Unhandled SkillTargetType: {skill.targetType} for skill {skill.skillNameKey}");
                break;
        }
        return finalTargets;
    }
}