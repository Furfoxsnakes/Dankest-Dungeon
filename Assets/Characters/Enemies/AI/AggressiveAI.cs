using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like ToList() and Any()
using DankestDungeon.Skills; // Required for SkillDefinitionSO and SkillTargetType

[CreateAssetMenu(fileName = "AggressiveAI", menuName = "Dankest Dungeon/AI/Aggressive")]
public class AggressiveAI : AIBehaviorSO
{
    [System.Serializable]
    public struct WeightedSkill
    {
        public SkillDefinitionSO skill;
        [Range(1, 100)]
        public int weight; // Higher weight means more likely to be chosen
    }

    [Tooltip("Overall chance to attempt using any skill from the AI Skill Pool (0-100).")]
    [Range(0, 100)]
    [SerializeField] private int attemptSkillChance = 80;

    [Header("AI Skill Pool")]
    [Tooltip("List of primary skills this AI can choose from, with associated weights.")]
    [SerializeField] private List<WeightedSkill> aiSkillPool = new List<WeightedSkill>();

    [Header("Fallback Options")]
    [Tooltip("The specific skill to use if the AI Skill Pool attempt fails or is skipped. Can be null to default to a very basic attack.")]
    [SerializeField] private SkillDefinitionSO designatedFallbackSkill;

    public override BattleAction DecideAction(Character self, List<Character> allies, List<Character> enemies)
    {
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();
        
        bool canTargetSelfOrAllyFromPool = aiSkillPool.Any(ws => ws.skill != null && 
                                                              (ws.skill.targetType == SkillTargetType.Self || 
                                                               ws.skill.targetType == SkillTargetType.SingleAlly || 
                                                               ws.skill.targetType == SkillTargetType.AllAllies));
        bool canFallbackTargetSelfOrAlly = designatedFallbackSkill != null && 
                                           (designatedFallbackSkill.targetType == SkillTargetType.Self ||
                                            designatedFallbackSkill.targetType == SkillTargetType.SingleAlly ||
                                            designatedFallbackSkill.targetType == SkillTargetType.AllAllies);

        if (aliveEnemies.Count == 0 && !canTargetSelfOrAllyFromPool && !canFallbackTargetSelfOrAlly)
        {
            Debug.LogWarning($"[AI] {self.GetName()}: No alive enemies and no self/ally skills (from pool or fallback) to use.");
            return null;
        }

        // 1. Attempt to use a skill from the AI Skill Pool
        if (Random.Range(0, 100) < attemptSkillChance)
        {
            List<WeightedSkill> usableWeightedSkills = new List<WeightedSkill>();
            foreach (var weightedSkillEntry in aiSkillPool)
            {
                // Ensure the skill from the pool is not the same as the designated fallback,
                // unless the fallback is the ONLY skill in the pool, to avoid it being overly preferred.
                // This simple check might need refinement based on desired interaction.
                if (weightedSkillEntry.skill != null && 
                    (weightedSkillEntry.skill != designatedFallbackSkill || aiSkillPool.Count == 1) && 
                    CanUseSkill(self, weightedSkillEntry.skill, allies, aliveEnemies))
                {
                    usableWeightedSkills.Add(weightedSkillEntry);
                }
            }

            if (usableWeightedSkills.Any())
            {
                SkillDefinitionSO chosenSkill = SelectSkillFromWeightedPool(usableWeightedSkills);
                
                if (chosenSkill != null)
                {
                    Character primaryTarget = DetermineTargetForSkill(self, chosenSkill, allies, aliveEnemies);
                    int skillRank = self.GetSkillRank(chosenSkill);
                    if (skillRank == 0 && chosenSkill.ranks.Any())
                    {
                        skillRank = 1; 
                    }

                    if (IsTargetRequired(chosenSkill.targetType) && primaryTarget == null)
                    {
                        Debug.LogWarning($"[AI] {self.GetName()}: Chosen skill from pool '{chosenSkill.skillNameKey}' requires a target, but none found. Proceeding to designated fallback.");
                    }
                    else if (skillRank > 0)
                    {
                        Debug.Log($"[AI] {self.GetName()} decided to use SKILL FROM POOL: {chosenSkill.skillNameKey} (Rank {skillRank}) on {primaryTarget?.GetName() ?? "area/self"}");
                        return new BattleAction(self, primaryTarget, chosenSkill); // Removed skillRank
                    }
                    else
                    {
                        Debug.LogWarning($"[AI] {self.GetName()}: Chosen skill from pool '{chosenSkill.skillNameKey}' has rank 0 and no default. Proceeding to designated fallback.");
                    }
                }
            }
            else
            {
                Debug.Log($"[AI] {self.GetName()}: Wanted to use a skill from pool, but no usable ones found. Proceeding to designated fallback.");
            }
        }
        else
        {
            Debug.Log($"[AI] {self.GetName()}: Skipped skill pool attempt due to chance ({attemptSkillChance}%). Proceeding to designated fallback.");
        }

        // 2. Attempt to use the Designated Fallback Skill
        if (designatedFallbackSkill != null)
        {
            if (CanUseSkill(self, designatedFallbackSkill, allies, aliveEnemies))
            {
                Character primaryTarget = DetermineTargetForSkill(self, designatedFallbackSkill, allies, aliveEnemies);
                int skillRank = self.GetSkillRank(designatedFallbackSkill);
                if (skillRank == 0 && designatedFallbackSkill.ranks.Any())
                {
                    skillRank = 1;
                }

                if (IsTargetRequired(designatedFallbackSkill.targetType) && primaryTarget == null)
                {
                    Debug.LogWarning($"[AI] {self.GetName()}: Designated fallback skill '{designatedFallbackSkill.skillNameKey}' requires a target, but none found. Proceeding to generic attack fallback.");
                }
                else if (skillRank > 0)
                {
                    Debug.Log($"[AI] {self.GetName()} decided to use DESIGNATED FALLBACK SKILL: {designatedFallbackSkill.skillNameKey} (Rank {skillRank}) on {primaryTarget?.GetName() ?? "area/self"}");
                    return new BattleAction(self, primaryTarget, designatedFallbackSkill); // Removed skillRank
                }
                else
                {
                     Debug.LogWarning($"[AI] {self.GetName()}: Designated fallback skill '{designatedFallbackSkill.skillNameKey}' has rank 0 and no default. Proceeding to generic attack fallback.");
                }
            }
            else
            {
                Debug.LogWarning($"[AI] {self.GetName()}: Designated fallback skill '{designatedFallbackSkill.skillNameKey}' cannot be used. Proceeding to generic attack fallback.");
            }
        }
        else
        {
            Debug.Log($"[AI] {self.GetName()}: No designated fallback skill set. Proceeding to generic attack fallback.");
        }

        // 3. Ultimate Fallback: Generic Attack
        return GetGenericFallbackAttack(self, aliveEnemies);
    }

    private SkillDefinitionSO SelectSkillFromWeightedPool(List<WeightedSkill> usableSkills)
    {
        if (usableSkills == null || !usableSkills.Any()) return null;

        int totalWeight = usableSkills.Sum(ws => ws.weight);
        if (totalWeight <= 0) 
        {
            return usableSkills[Random.Range(0, usableSkills.Count)].skill;
        }

        int randomNumber = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (var weightedSkill in usableSkills)
        {
            cumulativeWeight += weightedSkill.weight;
            if (randomNumber < cumulativeWeight)
            {
                return weightedSkill.skill;
            }
        }
        
        Debug.LogError("[AI] SelectSkillFromWeightedPool: Failed to select a skill, returning last in usable list as fallback.");
        return usableSkills.LastOrDefault().skill; 
    }
    
    private BattleAction GetGenericFallbackAttack(Character self, List<Character> aliveEnemies)
    {
        if (aliveEnemies.Count == 0)
        {
            Debug.Log($"[AI] {self.GetName()}: Generic fallback attack, but no alive enemies.");
            return null; 
        }
        Character target = FindLowestHealthTarget(aliveEnemies); 
         if (target == null) 
        {
            Debug.LogError($"[AI] {self.GetName()}: Generic fallback attack, FindLowestHealthTarget returned null despite alive enemies.");
            return null;
        }
        Debug.Log($"[AI] {self.GetName()} decided to use basic GENERIC ATTACK on {target.GetName()}");
        return new BattleAction(self, target, ActionType.Attack);
    }

    // --- Helper Methods (CanUseSkill, IsTargetRequired, DetermineTargetForSkill, FindLowestHealthTarget) ---
    // (These methods remain the same as in the previous version)
    private bool CanUseSkill(Character self, SkillDefinitionSO skill, List<Character> allies, List<Character> enemies)
    {
        if (skill == null) return false;
        switch (skill.targetType)
        {
            case SkillTargetType.None:
            case SkillTargetType.Self:
                return true; 
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.AllEnemies:
                return enemies.Any(e => e != null && e.IsAlive);
            case SkillTargetType.SingleAlly:
            case SkillTargetType.AllAllies:
                var potentialAllies = new List<Character>(allies);
                if (!potentialAllies.Contains(self)) potentialAllies.Add(self);
                return potentialAllies.Any(a => a != null && a.IsAlive);
            default:
                Debug.LogWarning($"[AI] CanUseSkill: Unhandled skill target type: {skill.targetType} for skill {skill.skillNameKey}. Assuming cannot use.");
                return false;
        }
    }
    
    private bool IsTargetRequired(SkillTargetType targetType)
    {
        switch (targetType)
        {
            case SkillTargetType.SingleEnemy:
            case SkillTargetType.SingleAlly:
                return true;
            case SkillTargetType.None:
            case SkillTargetType.Self:
            case SkillTargetType.AllEnemies: 
            case SkillTargetType.AllAllies:  
                return false; 
            default:
                return true; 
        }
    }

    private Character DetermineTargetForSkill(Character self, SkillDefinitionSO skill, List<Character> allies, List<Character> enemies)
    {
        var aliveAllies = allies.Where(a => a != null && a.IsAlive).ToList();
        if (!aliveAllies.Contains(self) && self.IsAlive) aliveAllies.Add(self); 

        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                return self;
            case SkillTargetType.SingleEnemy:
                return aliveEnemies.Any() ? FindLowestHealthTarget(aliveEnemies) : null;
            case SkillTargetType.AllEnemies:
                return aliveEnemies.Any() ? aliveEnemies.First() : null; 
            case SkillTargetType.SingleAlly:
                return aliveAllies.Any() ? FindLowestHealthTarget(aliveAllies.Where(a => a.CurrentHealth < a.Stats.maxHealth).ToList(), self) : null;
            case SkillTargetType.AllAllies:
                return aliveAllies.Any() ? aliveAllies.First() : null; 
            case SkillTargetType.None:
                return null;
            default:
                Debug.LogWarning($"[AI] DetermineTargetForSkill: Unhandled skill target type: {skill.targetType} for skill {skill.skillNameKey}. Returning null target.");
                return null;
        }
    }

    private Character FindLowestHealthTarget(List<Character> targets, Character defaultTargetIfEmpty = null)
    {
        if (targets == null || !targets.Any()) return defaultTargetIfEmpty;

        Character lowestHealthTarget = null;
        foreach(var t in targets)
        {
            if (t != null && t.IsAlive)
            {
                if (lowestHealthTarget == null || t.CurrentHealth < lowestHealthTarget.CurrentHealth)
                {
                    lowestHealthTarget = t;
                }
            }
        }
        return lowestHealthTarget ?? defaultTargetIfEmpty;
    }
}