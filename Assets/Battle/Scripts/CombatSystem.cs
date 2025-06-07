using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills;
using System.Linq; // For SkillEffectData, StatType, etc.


public class CombatSystem : MonoBehaviour
{
    [SerializeField] private SkillEffectProcessor skillProcessor;
    
    // Store references to character lists for damage calculations and validation
    private List<Character> playerCharacters;
    private List<Character> enemyCharacters;
    
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
        // Determine primary target for the animation/state, if applicable.
        // For multi-target skills, the animation might still be directed at the 'primary' selected target,
        // or it could be a generic animation not needing a specific target.
        Character primaryTargetForAnimation = action.Target; // Use the action's primary target for the animation state

        List<Character> finalEffectTargets = DetermineFinalTargets(actor, action.Target, skill.targetType);

        if (finalEffectTargets.Count == 0 && skill.targetType != SkillTargetType.Self && skill.targetType != SkillTargetType.None)
        {
            Debug.Log($"Skill {skill.skillNameKey} by {actor.GetName()} has no valid targets to affect.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"<color=green>[COMBAT] Executing Skill: {skill.skillNameKey} (Rank {action.SkillRank}) by {actor.GetName()} on {finalEffectTargets.Count} target(s). Primary anim target: {primaryTargetForAnimation?.GetName() ?? "None"}</color>");

        // Step 1: Play caster's skill animation
        if (skill.animationType != AnimationType.None)
        {
            // Pass the primary target if the animation type might need it for state setup (e.g., AttackState, MagicCastState)
            actor.PlayAnimation(skill.animationType, primaryTargetForAnimation);
            yield return new WaitUntil(() => actor.GetCurrentState() is IdleState);
            Debug.Log($"[COMBAT] {actor.GetName()} completed {skill.animationType} animation for skill {skill.skillNameKey}");
        }

        // Step 2: Process each effect for each target
        foreach (SkillEffectData effectData in currentRankData.effects)
        {
            // Determine targets for THIS specific effect, which might differ from the primary animation target
            // For simplicity, we're using finalEffectTargets determined earlier.
            // More complex skills might have per-effect targeting.
            foreach (Character effectTarget in finalEffectTargets)
            {
                if (effectTarget != null && effectTarget.IsAlive) // Use IsAlive property
                {
                    if (UnityEngine.Random.value > effectData.chance)
                    {
                        Debug.Log($"Effect {effectData.effectType} on {effectTarget.GetName()} failed chance roll ({effectData.chance * 100}%).");
                        continue; 
                    }
                    yield return StartCoroutine(ApplySkillEffectToTargetWithSequence(actor, effectTarget, effectData, currentRankData));
                }
            }
        }

        Debug.Log($"[COMBAT] Skill sequence complete for {skill.skillNameKey}");
        onComplete?.Invoke();
    }

    // Renamed and refactored from ApplySkillEffectToTarget
    private IEnumerator ApplySkillEffectToTargetWithSequence(Character caster, Character target, SkillEffectData effectData, SkillRankData rankData)
    {
        Debug.Log($"<color=lime>[COMBAT] Processing effect {effectData.effectType} from {caster.GetName()} to {target.GetName()}</color>");
        BattleUI battleUI = FindFirstObjectByType<BattleManager>()?.GetBattleUI(); // Get BattleUI if needed for floating text

        switch (effectData.effectType)
        {
            case SkillEffectType.Damage:
                // Play target's hit animation
                if (target != caster && target.IsAlive) // Use IsAlive property
                {
                    target.TakeHit(); // This method in Character should trigger its HitState
                    yield return new WaitUntil(() => target.GetCurrentState() is IdleState);
                    Debug.Log($"[COMBAT] Target ({target.GetName()}) completed hit animation.");
                }

                // Calculate and apply damage if target is still alive
                if (target.IsAlive) // Use IsAlive property
                {
                    DamageEffectResult damageResult = skillProcessor.CalculateDamageEffect(caster, target, effectData, rankData);
                    if (damageResult.success)
                    {
                        Debug.Log($"[COMBAT] Applying {damageResult.finalDamage} damage to {target.GetName()}. Crit: {damageResult.isCrit}");
                        target.TakeDamage(damageResult.finalDamage); // Ensure Character.cs has TakeDamage
                        // battleUI?.ShowFloatingText(target.transform.position, damageResult.finalDamage.ToString(), damageResult.isCrit ? Color.red : Color.white);
                        // if (damageResult.isCrit) battleUI?.ShowFloatingText(target.transform.position + Vector3.up * 0.5f, "CRIT!", Color.yellow);
                    }
                }
                break;

            case SkillEffectType.Heal:
                // Optional: Play target's "being healed" VFX/animation
                if (target.IsAlive || (effectData.effectType == SkillEffectType.Heal /* && isReviveSkill */)) // Use IsAlive property
                {
                    HealEffectResult healResult = skillProcessor.CalculateHealEffect(caster, target, effectData, rankData);
                    if (healResult.success)
                    {
                        Debug.Log($"[COMBAT] Applying {healResult.finalHeal} healing to {target.GetName()}. Crit: {healResult.isCrit}");
                        target.HealDamage(healResult.finalHeal); // Character method to increase health
                        // battleUI?.ShowFloatingText(target.transform.position, $"+{healResult.finalHeal}", Color.green);
                    }
                }
                break;

            case SkillEffectType.BuffStat:
            case SkillEffectType.DebuffStat:
                // Optional: Play target's "buff/debuff applied" VFX/animation
                if (target.IsAlive) // Use IsAlive property
                {
                    bool isBuff = effectData.effectType == SkillEffectType.BuffStat;
                    StatModEffectResult statModResult = skillProcessor.CalculateStatModificationEffect(caster, target, effectData, rankData, isBuff);
                    if (statModResult.success)
                    {
                        Debug.Log($"[COMBAT] Applying stat mod to {target.GetName()}: {statModResult.statToModify} by {statModResult.modValue} for {statModResult.duration} turns.");
                        target.AddTemporaryModifier(statModResult.statToModify, statModResult.modValue, statModResult.duration, statModResult.isBuff);
                        // battleUI?.ShowFloatingText(...);
                    }
                }
                break;

            case SkillEffectType.ApplyStatusEffect:
                // Optional: Play target's "status applied" VFX/animation
                 if (target.IsAlive) // Use IsAlive property
                {
                    StatusEffectApplicationResult statusResult = skillProcessor.CalculateStatusEffectApplication(caster, target, effectData, rankData);
                    if(statusResult.success)
                    {
                        Debug.Log($"[COMBAT] Applying status {statusResult.statusEffectName} to {target.GetName()} for {statusResult.duration} turns.");
                        // target.AddStatusEffect(statusResult.statusEffectToApply, statusResult.duration); // Actual application
                        // battleUI?.ShowFloatingText(...);
                    }
                }
                break;
            
            default:
                Debug.LogWarning($"[COMBAT] Effect type {effectData.effectType} not fully handled with sequence. Applying directly if possible.");
                // Fallback for simple effects without complex sequences - though ideally all are handled above
                // skillProcessor.ProcessEffect(caster, target, effectData, rankData, battleUI); // This old call should be avoided
                break;
        }
        // Small delay for visual clarity of the effect if not covered by animation waits
        yield return new WaitForSeconds(0.1f);
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

    private List<Character> DetermineFinalTargets(Character actor, Character primaryTarget, SkillTargetType skillTargetType)
    {
        List<Character> potentialTargets = new List<Character>();
        List<Character> alivePlayers = playerCharacters.Where(p => p != null && p.IsAlive).ToList(); // Use IsAlive property
        List<Character> aliveEnemies = enemyCharacters.Where(e => e != null && e.IsAlive).ToList(); // Use IsAlive property

        switch (skillTargetType)
        {
            case SkillTargetType.Self:
                if (actor != null && actor.IsAlive) potentialTargets.Add(actor); // Use IsAlive property
                break;

            case SkillTargetType.SingleEnemy:
                if (primaryTarget != null && primaryTarget.IsAlive && aliveEnemies.Contains(primaryTarget))
                    potentialTargets.Add(primaryTarget);
                // Fallback: if primary target is invalid but enemies exist, pick a random one (optional)
                else if (aliveEnemies.Any())
                {
                    Debug.LogWarning($"[TARGETING] SingleEnemy skill by {actor.GetName()} had invalid primary target. Targeting random enemy.");
                    potentialTargets.Add(aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)]);
                }
                break;

            case SkillTargetType.SingleAlly:
                if (primaryTarget != null && primaryTarget.IsAlive && alivePlayers.Contains(primaryTarget))
                    potentialTargets.Add(primaryTarget);
                // Fallback for allies (optional)
                else if (alivePlayers.Any())
                {
                     Debug.LogWarning($"[TARGETING] SingleAlly skill by {actor.GetName()} had invalid primary target. Targeting random ally.");
                    potentialTargets.Add(alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)]);
                }
                break;

            case SkillTargetType.AllEnemies:
                potentialTargets.AddRange(aliveEnemies);
                break;

            case SkillTargetType.AllAllies:
                potentialTargets.AddRange(alivePlayers);
                break;

            case SkillTargetType.RandomEnemy:
                if (aliveEnemies.Any())
                    potentialTargets.Add(aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)]);
                break;

            case SkillTargetType.RandomAlly:
                if (alivePlayers.Any())
                    potentialTargets.Add(alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)]);
                break;
            
            // TODO: Implement Row targeting if you have a FormationManager or similar system
            // case SkillTargetType.EnemyRow:
            // case SkillTargetType.AllyRow:
            //    // This would require knowing character positions/rows
            //    break;

            default:
                Debug.LogWarning($"[TARGETING] Unhandled SkillTargetType: {skillTargetType}. Defaulting to primary target if valid.");
                if (primaryTarget != null && primaryTarget.IsAlive)
                    potentialTargets.Add(primaryTarget);
                break;
        }
        // Ensure all targets are distinct and still alive (double check)
        return potentialTargets.Where(t => t != null && t.IsAlive).Distinct().ToList(); // Use IsAlive property
    }

    // ... rest of CombatSystem methods ...
}