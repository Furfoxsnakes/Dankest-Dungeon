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
        [Range(1, 5)] // Assuming max 5 ranks, adjust as needed
        public int rankToUse; // 1-based rank level
        [Range(1, 100)]
        public int weight; // Higher weight means more likely to be chosen
    }

    [System.Serializable]
    public struct FallbackSkillInfo
    {
        public SkillDefinitionSO skill;
        [Range(1, 5)] // Assuming max 5 ranks, adjust as needed
        public int rankToUse; // 1-based rank level
    }

    [Tooltip("Overall chance to attempt using any skill from the AI Skill Pool (0-100).")]
    [Range(0, 100)]
    [SerializeField] private int attemptSkillChance = 80;

    [Header("AI Skill Pool")]
    [Tooltip("List of primary skills this AI can choose from, with associated weights and ranks.")]
    [SerializeField] private List<WeightedSkill> aiSkillPool = new List<WeightedSkill>();

    [Header("Fallback Options")]
    [Tooltip("The specific skill and its rank to use if the AI Skill Pool attempt fails or is skipped. Skill can be null to default to a very basic attack.")]
    [SerializeField] private FallbackSkillInfo designatedFallbackSkill; // Changed type

    public override BattleAction DecideAction(Character self, List<Character> allies, List<Character> enemies)
    {
        var aliveEnemies = enemies.Where(e => e != null && e.IsAlive).ToList();
        
        bool canTargetSelfOrAllyFromPool = aiSkillPool.Any(ws => ws.skill != null && 
                                                              (ws.skill.targetType == SkillTargetType.Self || 
                                                               ws.skill.targetType == SkillTargetType.SingleAlly || 
                                                               ws.skill.targetType == SkillTargetType.AllAllies));
        
        // Updated to check the skill within FallbackSkillInfo
        bool canFallbackTargetSelfOrAlly = designatedFallbackSkill.skill != null && 
                                           (designatedFallbackSkill.skill.targetType == SkillTargetType.Self ||
                                            designatedFallbackSkill.skill.targetType == SkillTargetType.SingleAlly ||
                                            designatedFallbackSkill.skill.targetType == SkillTargetType.AllAllies);

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
                if (weightedSkillEntry.skill != null && 
                    (weightedSkillEntry.skill != designatedFallbackSkill.skill || aiSkillPool.Count == 1) && 
                    CanUseSkill(self, weightedSkillEntry.skill, allies, aliveEnemies) && // Basic check
                    weightedSkillEntry.rankToUse > 0 && weightedSkillEntry.rankToUse -1 < weightedSkillEntry.skill.ranks.Count) // Rank validity
                {
                    // Potentially add mana check here for weightedSkillEntry.skill.ranks[weightedSkillEntry.rankToUse - 1].manaCost
                    usableWeightedSkills.Add(weightedSkillEntry);
                }
            }

            if (usableWeightedSkills.Any())
            {
                WeightedSkill chosenWeightedSkill = SelectSkillFromWeightedPool(usableWeightedSkills); // Now returns WeightedSkill
                
                if (chosenWeightedSkill.skill != null)
                {
                    SkillDefinitionSO chosenSkillDef = chosenWeightedSkill.skill;
                    int chosenRank = chosenWeightedSkill.rankToUse;

                    Character primaryTarget = DetermineTargetForSkill(self, chosenSkillDef, allies, aliveEnemies);

                    if (IsTargetRequired(chosenSkillDef.targetType) && primaryTarget == null)
                    {
                        Debug.LogWarning($"[AI] {self.GetName()}: Chosen skill from pool '{chosenSkillDef.skillNameKey}' Rank {chosenRank} requires a target, but none found. Proceeding to designated fallback.");
                    }
                    // Rank validity already checked when adding to usableWeightedSkills
                    else if (chosenRank > 0 && chosenRank -1 < chosenSkillDef.ranks.Count) 
                    {
                        SkillRankData chosenSkillRankData = chosenSkillDef.ranks[chosenRank - 1];
                        Debug.Log($"[AI] {self.GetName()} decided to use SKILL FROM POOL: {chosenSkillDef.skillNameKey} (Rank {chosenRank}) on {primaryTarget?.GetName() ?? "area/self"}");
                        return new BattleAction(self, primaryTarget, chosenSkillDef, chosenSkillRankData);
                    }
                    else // Should ideally not be reached if usableWeightedSkills logic is correct
                    {
                        Debug.LogError($"[AI] {self.GetName()}: Skill '{chosenSkillDef.skillNameKey}' rank {chosenRank} is invalid. Fallback.");
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
        if (designatedFallbackSkill.skill != null) // Check if a fallback skill is assigned
        {
            SkillDefinitionSO fallbackSkillDef = designatedFallbackSkill.skill;
            int fallbackRank = designatedFallbackSkill.rankToUse;

            // Validate the rank for the fallback skill
            if (fallbackRank <= 0 || fallbackRank - 1 >= fallbackSkillDef.ranks.Count)
            {
                Debug.LogError($"[AI] {self.GetName()}: Designated fallback skill '{fallbackSkillDef.skillNameKey}' has an invalid rank configured ({fallbackRank}). Max ranks: {fallbackSkillDef.ranks.Count}. Proceeding to generic attack.");
            }
            else if (CanUseSkill(self, fallbackSkillDef, allies, aliveEnemies)) // Basic check
            {
                // Potentially add mana check here for fallbackSkillDef.ranks[fallbackRank - 1].manaCost
                Character primaryTarget = DetermineTargetForSkill(self, fallbackSkillDef, allies, aliveEnemies);

                if (IsTargetRequired(fallbackSkillDef.targetType) && primaryTarget == null)
                {
                    Debug.LogWarning($"[AI] {self.GetName()}: Designated fallback skill '{fallbackSkillDef.skillNameKey}' Rank {fallbackRank} requires a target, but none found. Proceeding to generic attack fallback.");
                }
                else
                {
                    SkillRankData fallbackSkillRankData = fallbackSkillDef.ranks[fallbackRank - 1];
                    Debug.Log($"[AI] {self.GetName()} decided to use DESIGNATED FALLBACK SKILL: {fallbackSkillDef.skillNameKey} (Rank {fallbackRank}) on {primaryTarget?.GetName() ?? "area/self"}");
                    return new BattleAction(self, primaryTarget, fallbackSkillDef, fallbackSkillRankData);
                }
            }
            else
            {
                Debug.LogWarning($"[AI] {self.GetName()}: Designated fallback skill '{fallbackSkillDef.skillNameKey}' Rank {fallbackRank} cannot be used (e.g., no valid targets). Proceeding to generic attack fallback.");
            }
        }
        else
        {
            Debug.Log($"[AI] {self.GetName()}: No designated fallback skill set. Proceeding to generic attack fallback.");
        }

        // 3. Ultimate Fallback: Generic Attack
        return GetGenericFallbackAttack(self, aliveEnemies);
    }

    // Modified to return WeightedSkill to get rank info too
    private WeightedSkill SelectSkillFromWeightedPool(List<WeightedSkill> usableSkills)
    {
        if (usableSkills == null || !usableSkills.Any()) return default; // Return default WeightedSkill

        int totalWeight = usableSkills.Sum(ws => ws.weight);
        if (totalWeight <= 0) 
        {
            // Fallback to random if weights are zero or negative
            return usableSkills[Random.Range(0, usableSkills.Count)];
        }

        int randomNumber = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        foreach (var weightedSkill in usableSkills)
        {
            cumulativeWeight += weightedSkill.weight;
            if (randomNumber < cumulativeWeight)
            {
                return weightedSkill;
            }
        }
        
        Debug.LogError("[AI] SelectSkillFromWeightedPool: Failed to select a skill based on weights, returning last in usable list as fallback.");
        return usableSkills.LastOrDefault(); 
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
            // This case should ideally be prevented by the aliveEnemies check,
            // but FindLowestHealthTarget might return null if all alive enemies are somehow invalid for targeting.
            Debug.LogError($"[AI] {self.GetName()}: Generic fallback attack, FindLowestHealthTarget returned null despite alive enemies. This might indicate an issue in target validation.");
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