using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills;
using System.Linq; // Make sure LINQ is included
using Random = UnityEngine.Random; // Use Unity's Random for consistency


public class CombatSystem : MonoBehaviour
{
    [SerializeField] private SkillEffectProcessor skillProcessor;
    
    // Store references to character lists for damage calculations and validation
    private List<Character> playerCharacters;
    private List<Character> enemyCharacters;
    
    // These should be populated by BattleManager or another central system
    private List<Character> _playerTeamCharacters;
    private List<Character> _enemyTeamCharacters;

    private void Awake()
    {
        if (skillProcessor == null)
            skillProcessor = GetComponent<SkillEffectProcessor>() ?? gameObject.AddComponent<SkillEffectProcessor>();
    }
    
    public void Initialize(List<Character> players, List<Character> enemies)
    {
        playerCharacters = players;
        enemyCharacters = enemies;
        
        Debug.Log($"[COMBAT] Combat system initialized with {players.Count} players and {enemies.Count} enemies");
        
        foreach (var character in players) { ValidateCharacter(character, "Player"); }
        foreach (var character in enemies) { ValidateCharacter(character, "Enemy"); }
    }

    public void InitializeTeams(List<Character> playerTeam, List<Character> enemyTeam)
    {
        _playerTeamCharacters = playerTeam;
        _enemyTeamCharacters = enemyTeam;
    }
    
    private void ValidateCharacter(Character character, string type)
    {
        if (character == null) { Debug.LogError($"[COMBAT] Null {type} character found during initialization!"); return; }
        if (!character.IsAlive) // Use IsAlive property
        {
            Debug.LogWarning($"[COMBAT] {type} character {character.GetName()} is not alive at battle start!");
        }
        
        // Validate required components for combat
        if (character.characterAnimator == null)
        {
            Debug.LogWarning($"[COMBAT] {type} character {character.GetName()} has no animator!");
        }
        
        Debug.Log($"[COMBAT] {type} character {character.GetName()} validated for combat");
    }
    
    // Getter methods for accessing character lists if needed
    public List<Character> GetPlayerCharacters() => playerCharacters;
    public List<Character> GetEnemyCharacters() => enemyCharacters;
    
    public void ExecuteAction(BattleAction action, Action onComplete)
    {
        Debug.Log($"[COMBAT] Executing {action.ActionType}: {action.Actor.GetName()} -> {action.Target?.GetName() ?? "self"}");
        
        switch (action.ActionType)
        {
            case ActionType.Attack:
                StartCoroutine(ExecuteAttackSequence(action.Actor, action.Target, onComplete));
                break;
                
            case ActionType.Defend:
                StartCoroutine(ExecuteDefendSequence(action.Actor, onComplete));
                break;
                
            case ActionType.Magic:
                StartCoroutine(ExecuteMagicSequence(action.Actor, action.Target, onComplete));
                break;
                
            case ActionType.Item:
                StartCoroutine(ExecuteItemSequence(action.Actor, action.Target, onComplete));
                break;
                
            case ActionType.Skill: // Add this new case
                StartCoroutine(ExecuteSkillSequence(action, onComplete));
                break;
                
            default:
                Debug.LogWarning($"Unknown action type: {action.ActionType}");
                onComplete?.Invoke();
                break;
        }
    }
    
    private IEnumerator ExecuteAttackSequence(Character attacker, Character target, Action onComplete)
    {
        Debug.Log($"[COMBAT] Starting attack sequence: {attacker.GetName()} attacks {target.GetName()}");

        // Step 1: Attacker plays attack animation.
        attacker.Attack(target); // This will trigger the Character's AttackState.

        // Wait for the attacker to finish their AttackState and return to Idle.
        yield return new WaitUntil(() => attacker.GetCurrentState() is IdleState);
        Debug.Log($"[COMBAT] Attacker ({attacker.GetName()}) has returned to Idle. Proceeding with sequence.");

        // Step 2: Target plays hit animation (if alive)
        if (!target.IsAlive) // Use IsAlive property
        {
            Debug.Log($"[COMBAT] Target ({target.GetName()}) is no longer alive. Skipping hit animation and damage.");
            Debug.Log($"[COMBAT] Attack sequence complete (target died before hit).");
            onComplete?.Invoke();
            yield break; 
        }

        target.TakeHit(); // This will trigger the Character's HitState.
        
        // Wait for the target to finish their HitState and return to Idle.
        yield return new WaitUntil(() => target.GetCurrentState() is IdleState);
        Debug.Log($"[COMBAT] Target ({target.GetName()}) has returned to Idle after hit. Proceeding with damage.");

        // Step 3: Apply damage to target (if still alive after animations)
        // It's good practice to check IsAlive again, though in this flow it should be.
        if (!target.IsAlive) // Use IsAlive property
        {
            Debug.Log($"[COMBAT] Target ({target.GetName()}) died during hit animation. Skipping further damage application.");
            Debug.Log($"[COMBAT] Attack sequence complete (target died during hit).");
            onComplete?.Invoke();
            yield break;
        }

        int damage = CalculateDamage(attacker, target); // USE THIS METHOD
        Debug.Log($"[COMBAT] Applying {damage} damage to {target.GetName()}.");
        target.TakeDamage(damage); // Ensure Character.cs has TakeDamage

        // Step 4: Sequence complete
        Debug.Log($"[COMBAT] Attack sequence complete for {attacker.GetName()} against {target.GetName()}.");
        onComplete?.Invoke();
    }
    
    private IEnumerator ExecuteDefendSequence(Character defender, Action onComplete)
    {
        Debug.Log($"[COMBAT] {defender.GetName()} defends");

        // Step 1: Defender plays defend animation.
        // Assuming defender.Defend() triggers a DefendState which plays an animation
        // and eventually returns the character to IdleState.
        defender.Defend(); 

        // Wait for the defender to finish their DefendState and return to Idle.
        yield return new WaitUntil(() => defender.GetCurrentState() is IdleState);
        Debug.Log($"[COMBAT] Defender ({defender.GetName()}) has returned to Idle after defending.");

        // Step 2: Apply defense effects
        defender.ApplyDefenseStance();
        
        Debug.Log($"[COMBAT] Defend sequence complete for {defender.GetName()}");
        onComplete?.Invoke();
    }
    
    private IEnumerator ExecuteMagicSequence(Character caster, Character target, Action onComplete)
    {
        Debug.Log($"[COMBAT] {caster.GetName()} casts magic on {target?.GetName() ?? "area/self"}");
        
        // Step 1: Caster plays cast animation.
        // Assuming caster.CastMagic(target) triggers a MagicState or similar.
        caster.CastMagic(target); 
        
        // Wait for the caster to finish their MagicState and return to Idle.
        yield return new WaitUntil(() => caster.GetCurrentState() is IdleState);
        Debug.Log($"[COMBAT] Caster ({caster.GetName()}) has returned to Idle after casting magic.");

        // Step 2: If there's a target, target plays hit/effect animation (if alive).
        if (target != null) // Check if there's a target for the magic
        {
            if (!target.IsAlive) // Use IsAlive property
            {
                Debug.Log($"[COMBAT] Magic target ({target.GetName()}) is no longer alive. Skipping hit animation and damage.");
            }
            else
            {
                // Assuming magic might have its own effect animation or re-use TakeHit
                target.TakeHit(); // Or a new method like target.PlayMagicEffectAnimation();
            
                // Wait for the target to finish their animation and return to Idle.
                yield return new WaitUntil(() => target.GetCurrentState() is IdleState);
                Debug.Log($"[COMBAT] Target ({target.GetName()}) has returned to Idle after magic effect.");
            }
        }
        else
        {
            Debug.Log($"[COMBAT] Magic has no direct target or is self-cast/area effect. Skipping target animation phase.");
        }
        
        // Step 3: Apply magic damage/effects (if target is still alive or if it's an area/self effect)
        if (target != null)
        {
            if (target.IsAlive) // Use IsAlive property
            {
                int damage = CalculateMagicDamage(caster, target); // USE THIS METHOD
                Debug.Log($"[COMBAT] Applying {damage} magic damage to {target.GetName()}.");
                target.TakeDamage(damage); // Ensure Character.cs has TakeDamage
            }
        }
        else
        {
            // Handle area effects or self-buffs here if your magic system supports it
            Debug.Log($"[COMBAT] Applying non-targeted magic effects for {caster.GetName()}.");
            // Example: caster.ApplySelfBuff(someSpellEffect);
        }
        
        Debug.Log($"[COMBAT] Magic sequence complete for {caster.GetName()}.");
        onComplete?.Invoke();
    }
    
    private IEnumerator ExecuteItemSequence(Character user, Character target, Action onComplete)
    {
        Debug.Log($"[COMBAT] {user.GetName()} uses item on {target?.GetName() ?? "self/area"}");
        
        // Step 1: User plays item use animation.
        // Assuming user.UseItem(target) triggers an ItemState or similar.
        user.UseItem(target);

        // Wait for the user to finish their ItemState and return to Idle.
        yield return new WaitUntil(() => user.GetCurrentState() is IdleState);
        Debug.Log($"[COMBAT] User ({user.GetName()}) has returned to Idle after using item.");

        // Step 2: If there's a target, target plays effect animation (if alive and item has visual effect).
        if (target != null) // Check if the item has a target
        {
            if (!target.IsAlive)
            {
                Debug.Log($"[COMBAT] Item target ({target.GetName()}) is no longer alive. Skipping effect animation and application.");
            }
            else
            {
                // You might have a specific method for item effects or re-use TakeHit
                // For example, if it's a healing item, it might play a "HealEffect" animation.
                // For now, let's assume a generic effect or re-use TakeHit if it's a damaging item.
                // target.PlayItemEffectAnimation(); // Or target.TakeHit() if damaging
                Debug.Log($"[COMBAT] Item target ({target.GetName()}) would play effect animation here if designed.");
                // If an animation is played:
                // yield return new WaitUntil(() => target.GetCurrentState() is IdleState);
                // Debug.Log($"[COMBAT] Target ({target.GetName()}) has returned to Idle after item effect.");
            }
        }
        else
        {
            Debug.Log($"[COMBAT] Item has no direct target or is self-use. Skipping target animation phase.");
        }

        // Step 3: Apply item effects (damage, healing, status changes)
        // This logic would depend heavily on your item system.
        // Example:
        // ItemData itemUsed = user.GetLastUsedItem(); // Hypothetical
        // if (itemUsed != null) {
        //     itemUsed.ApplyEffect(user, target);
        // }
        Debug.Log($"[COMBAT] Applying item effects for {user.GetName()}.");


        Debug.Log($"[COMBAT] Item sequence complete for {user.GetName()}.");
        onComplete?.Invoke();
    }
    
    private IEnumerator ExecuteSkillSequence(BattleAction action, Action onComplete)
    {
        SkillDefinitionSO skill = action.UsedSkill;
        Character actor = action.Actor;
        int rankIndex = action.SkillRank - 1;

        if (rankIndex < 0 || rankIndex >= skill.ranks.Count)
        {
            Debug.LogError($"Invalid skill rank {action.SkillRank} for {skill.skillNameKey}. Max ranks: {skill.ranks.Count}");
            onComplete?.Invoke();
            yield break;
        }
        
        SkillRankData currentRankData = skill.ranks[rankIndex];
        Character primaryTargetForAnimation = action.Target;

        // MODIFIED CALL HERE: Pass the 'skill' (SkillDefinitionSO) instead of 'skill.targetType'
        // This will call your newer, context-aware DetermineFinalTargets method.
        List<Character> finalEffectTargets = DetermineFinalTargets(actor, action.Target, skill);

        if (finalEffectTargets.Count == 0 && skill.targetType != SkillTargetType.Self && skill.targetType != SkillTargetType.None)
        {
            Debug.Log($"Skill {skill.skillNameKey} by {actor.GetName()} has no valid targets to affect.");
            // It's possible the animation still plays if it's non-targeted,
            // but effects won't apply. Consider if onComplete should be called here or after animation.
            // For now, let's assume if no effect targets, the core of the skill fails.
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"<color=green>[COMBAT] Executing Skill: {skill.skillNameKey} (Rank {action.SkillRank}) by {actor.GetName()} on {finalEffectTargets.Count} target(s). Primary anim target: {primaryTargetForAnimation?.GetName() ?? "None"}</color>");

        // Step 1: Play caster's skill animation
        if (skill.animationType != AnimationType.None)
        {
            actor.PlayAnimation(skill.animationType, primaryTargetForAnimation);
            yield return new WaitUntil(() => actor.GetCurrentState() is IdleState);
            Debug.Log($"[COMBAT] {actor.GetName()} completed {skill.animationType} animation for skill {skill.skillNameKey}");
        }

        // Step 2: Process each effect for each target
        foreach (SkillEffectData effectData in currentRankData.effects)
        {
            foreach (Character effectTarget in finalEffectTargets)
            {
                if (effectTarget != null && effectTarget.IsAlive) 
                {
                    if (UnityEngine.Random.value > effectData.chance)
                    {
                        Debug.Log($"Effect {effectData.effectType} on {effectTarget.GetName()} failed chance roll ({effectData.chance * 100}%).");
                        continue; 
                    }
                    yield return StartCoroutine(ApplySkillEffectToTargetWithSequence(actor, effectTarget, effectData, currentRankData, skill));
                }
            }
        }

        Debug.Log($"[COMBAT] Skill sequence complete for {skill.skillNameKey}");
        onComplete?.Invoke();
    }

    // Renamed and refactored from ApplySkillEffectToTarget
    private IEnumerator ApplySkillEffectToTargetWithSequence(Character actor, Character targetCharacter, SkillEffectData effectData, SkillRankData skillRankData, SkillDefinitionSO skill)
    {
        if (targetCharacter == null)
        {
            Debug.LogError($"[COMBAT] Target character is null for effect {effectData.effectType} from skill {skill.skillNameKey}.");
            yield break;
        }
        if (effectData == null)
        {
            Debug.LogError($"[COMBAT] SkillEffectData is null for skill {skill.skillNameKey}.");
            yield break;
        }

        Debug.Log($"[COMBAT] Processing effect '{effectData.effectType}' from skill '{skill.skillNameKey}' on target '{targetCharacter.GetName()}' by actor '{actor.GetName()}'");

        // Play target's hit animation if applicable (and alive)
        // Adjust this condition based on which effects should trigger a "hit" reaction
        bool shouldPlayHitAnimation = effectData.effectType == SkillEffectType.Damage ||
                                      effectData.effectType == SkillEffectType.DebuffStat; // Example: Debuffs also cause a hit reaction

        if (shouldPlayHitAnimation && targetCharacter.IsAlive)
        {
            targetCharacter.TakeHit(); 
            yield return new WaitUntil(() => !(targetCharacter.GetCurrentState() is HitState) || !targetCharacter.IsAlive);
            Debug.Log($"[COMBAT] Target ({targetCharacter.GetName()}) completed hit animation sequence or died.");
        }

        if (!targetCharacter.IsAlive && effectData.effectType != SkillEffectType.Revive) 
        {
            Debug.Log($"[COMBAT] Target {targetCharacter.GetName()} is dead. Skipping effect {effectData.effectType} for skill {skill.skillNameKey} (unless revive).");
            yield break;
        }

        // Initialize with a default value or new instance.
        // The 'success' field will indicate if the calculation was valid.
        DamageEffectResult damageEffectResult = default; // Or new DamageEffectResult();

        switch (effectData.effectType)
        {
            case SkillEffectType.Damage:
                // Call the CalculateDamageEffect that returns DamageEffectResult
                damageEffectResult = skillProcessor.CalculateDamageEffect(actor, targetCharacter, effectData, skillRankData);
                
                // Check the 'success' field instead of null
                if (damageEffectResult.success)
                {
                    Debug.Log($"[COMBAT] Applying {damageEffectResult.finalDamage} damage to {targetCharacter.GetName()}. Crit: {damageEffectResult.isCrit}");
                    targetCharacter.TakeDamage(damageEffectResult.finalDamage);
                }
                else
                {
                    Debug.Log($"[COMBAT] Damage calculation failed or was skipped for {targetCharacter.GetName()}.");
                }
                break;

            case SkillEffectType.Heal:
                // Calculate final heal amount, potentially using effectData.baseValue, scalingStat, etc.
                // Assuming CalculateValueWithScaling is correctly in SkillEffectProcessor
                // For HealEffectResult, you'd do a similar change if it's also a struct.
                // Let's assume skillProcessor.CalculateHealEffect returns a HealEffectResult struct:
                HealEffectResult healEffectResult = skillProcessor.CalculateHealEffect(actor, targetCharacter, effectData, skillRankData);
                if (healEffectResult.success)
                {
                    Debug.Log($"[COMBAT] Healing {targetCharacter.GetName()} for {healEffectResult.finalHeal}. Crit: {healEffectResult.isCrit}");
                    targetCharacter.HealDamage(healEffectResult.finalHeal);
                }
                else
                {
                     Debug.Log($"[COMBAT] Heal calculation failed or was skipped for {targetCharacter.GetName()}.");
                }
                break;

            case SkillEffectType.BuffStat: // Explicitly a buff to a stat
            case SkillEffectType.DebuffStat: // Explicitly a debuff to a stat
                if (targetCharacter.IsAlive || (targetCharacter.GetCurrentState() is DeathState && effectData.statToModify == StatType.MaxHealth))
                {
                    // Calculate the actual modifier value, potentially including scaling
                    // Assuming CalculateValueWithScaling is correctly in SkillEffectProcessor
                    float modifierActualValue = skillProcessor.CalculateValueWithScaling(actor, effectData.baseValue, effectData.scalingStat, effectData.scalingMultiplier);

                    Character.TemporaryModifier newModifier = new Character.TemporaryModifier
                    {
                        statType = effectData.statToModify,
                        value = modifierActualValue, // Use the calculated value
                        duration = effectData.duration,
                        isBuff = (effectData.effectType == SkillEffectType.BuffStat), // BuffStat is a buff, DebuffStat is not.
                        sourceName = skill.skillNameKey 
                    };
                    targetCharacter.AddTemporaryModifier(newModifier);
                    Debug.Log($"[COMBAT] Applied modifier {newModifier.statType} from {newModifier.sourceName} to {targetCharacter.GetName()}. Value: {newModifier.value}, Duration: {newModifier.duration}, IsBuff: {newModifier.isBuff}");
                }
                else
                {
                    Debug.Log($"[COMBAT] Skipped applying stat modifier from {skill.skillNameKey} to {targetCharacter.GetName()} because target is dead and conditions not met.");
                }
                break;
            
            // case SkillEffectType.ApplyStatusEffect:
            //    // TODO: Implement logic to apply status effects
            //    Debug.Log($"[COMBAT] Applying status effect (not yet implemented) from {skill.skillNameKey} to {targetCharacter.GetName()}.");
            //    break;

            // case SkillEffectType.ClearStatusEffect:
            //    // TODO: Implement logic to clear status effects
            //    Debug.Log($"[COMBAT] Clearing status effect (not yet implemented) from {skill.skillNameKey} on {targetCharacter.GetName()}.");
            //    break;
            
            // case SkillEffectType.Revive:
            //    // TODO: Implement revive logic
            //    // float reviveHealthPercentage = effectData.baseValue; // e.g., baseValue is 0.25 for 25% health
            //    // targetCharacter.Revive(reviveHealthPercentage);
            //    Debug.Log($"[COMBAT] Reviving (not yet implemented) {targetCharacter.GetName()} with skill {skill.skillNameKey}.");
            //    break;

            default:
                Debug.LogWarning($"[COMBAT] Unhandled SkillEffectType: {effectData.effectType} for skill {skill.skillNameKey}");
                break;
        }
        yield return null; 
    }

    // --- ADDED IMPLEMENTATIONS ---

    private int CalculateDamage(Character attacker, Character target)
    {
        if (attacker == null || target == null) return 0;

        // Basic damage calculation: Attacker's Attack Power - Target's Defense
        // You can expand this with critical hits, randomness, elemental weaknesses/resistances, etc.
        float attackPower = attacker.GetAttackPower(); // Assuming Character has GetAttackPower()
        float defense = target.GetDefense();       // Assuming Character has GetDefense()

        int damage = Mathf.RoundToInt(Mathf.Max(1, attackPower - defense)); // Ensure at least 1 damage
        
        // Example: Add a small random variance
        // damage = Mathf.RoundToInt(damage * Random.Range(0.9f, 1.1f));
        // damage = Mathf.Max(1, damage); // Ensure it's still at least 1 after variance

        Debug.Log($"[COMBAT_CALC] {attacker.GetName()} (AP:{attackPower}) vs {target.GetName()} (Def:{defense}) = Base Damage: {damage}");
        return damage;
    }

    private int CalculateMagicDamage(Character caster, Character target)
    {
        if (caster == null || target == null) return 0;

        // Basic magic damage: Caster's Magic Power - Target's Magic Resistance
        float magicPower = caster.GetMagicPower();         // Assuming Character has GetMagicPower()
        float magicResistance = target.GetMagicResistance(); // Assuming Character has GetMagicResistance()

        int damage = Mathf.RoundToInt(Mathf.Max(1, magicPower - magicResistance));
        
        Debug.Log($"[COMBAT_CALC] {caster.GetName()} (MP:{magicPower}) vs {target.GetName()} (MR:{magicResistance}) = Base Magic Damage: {damage}");
        return damage;
    }

    private List<Character> DetermineFinalTargets(Character actor, Character primaryTarget, SkillDefinitionSO skill)
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

    // ... rest of CombatSystem methods ...
}