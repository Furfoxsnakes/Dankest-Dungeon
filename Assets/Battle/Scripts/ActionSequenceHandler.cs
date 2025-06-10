using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills; // Assuming this namespace for Skill, SkillEffectData etc.

public class ActionSequenceHandler : MonoBehaviour
{
    private CombatSystem combatSystem; // To access character lists or other CombatSystem methods if needed
    private SkillEffectProcessor skillProcessor;
    private TargetingSystem targetingSystem;
    private int _activeSequences = 0; // Ensure this is declared

    public void Initialize(CombatSystem cs, SkillEffectProcessor sp, TargetingSystem ts)
    {
        combatSystem = cs;
        skillProcessor = sp;
        targetingSystem = ts;
    }

    public void ExecuteAction(BattleAction action, Action onComplete)
    {
        // Use PrimaryTarget for logging the initial target focus
        Debug.Log($"[COMBAT] Executing {action.ActionType}: {action.Actor.GetName()} -> {action.PrimaryTarget?.GetName() ?? "self/area"}");

        switch (action.ActionType)
        {
            case ActionType.Attack:
                // Pass PrimaryTarget for single-target actions
                StartCoroutine(ExecuteAttackSequence(action.Actor, action.PrimaryTarget, onComplete));
                break;

            case ActionType.Defend:
                StartCoroutine(ExecuteDefendSequence(action.Actor, onComplete));
                break;

            case ActionType.Magic:
                // Pass PrimaryTarget for single-target actions
                StartCoroutine(ExecuteMagicSequence(action.Actor, action.PrimaryTarget, onComplete));
                break;

            case ActionType.Item:
                // Pass PrimaryTarget for single-target actions
                StartCoroutine(ExecuteItemSequence(action.Actor, action.PrimaryTarget, onComplete));
                break;

            case ActionType.Skill:
                if (action.UsedSkill != null && action.SkillRank != null)
                {
                    SkillDefinitionSO skillDef = action.UsedSkill;
                    Character actorChar = action.Actor;
                    List<Character> skillTargetsToProcess;
                    Character animationFocusTarget = action.PrimaryTarget; // Default animation focus to the primary selected target

                    // Prioritize ResolvedTargets if available
                    if (action.ResolvedTargets != null && action.ResolvedTargets.Count > 0)
                    {
                        skillTargetsToProcess = action.ResolvedTargets;
                        Debug.Log($"[ActionSequenceHandler] Using pre-resolved targets for skill '{skillDef.skillNameKey}'. Count: {skillTargetsToProcess.Count}");
                    }
                    else
                    {
                        // If ResolvedTargets is not set, determine them now using the PrimaryTarget as a hint
                        Debug.LogWarning($"[ActionSequenceHandler] action.ResolvedTargets was null or empty for skill '{skillDef.skillNameKey}'. Resolving now using PrimaryTarget: {action.PrimaryTarget?.GetName() ?? "null"}.");
                        skillTargetsToProcess = targetingSystem.DetermineFinalTargets(actorChar, action.PrimaryTarget, skillDef);
                        action.ResolvedTargets = skillTargetsToProcess; // Store the resolved targets back into the action
                    }

                    // Determine the target for animation focus
                    if (animationFocusTarget == null && skillTargetsToProcess != null && skillTargetsToProcess.Count > 0)
                    {
                        animationFocusTarget = skillTargetsToProcess[0]; // Fallback to the first resolved target if no primary target
                        Debug.Log($"[ActionSequenceHandler] PrimaryTarget for animation was null, using first resolved target: {animationFocusTarget.GetName()} for skill '{skillDef.skillNameKey}'.");
                    }
                    
                    if (skillDef.targetType == SkillTargetType.Self)
                    {
                        animationFocusTarget = actorChar; // Self-targeted skills always animate on the actor
                        // Ensure the actor is in the list of targets for self-targeted skills
                        if (skillTargetsToProcess == null || !skillTargetsToProcess.Contains(actorChar))
                        {
                            skillTargetsToProcess = new List<Character> { actorChar };
                            action.ResolvedTargets = skillTargetsToProcess;
                             Debug.Log($"[ActionSequenceHandler] Skill '{skillDef.skillNameKey}' is self-targeted. Ensured actor is the target.");
                        }
                    }
                    
                    // Ensure skillTargetsToProcess is not null before passing
                    if (skillTargetsToProcess == null)
                    {
                        skillTargetsToProcess = new List<Character>();
                        Debug.LogWarning($"[ActionSequenceHandler] skillTargetsToProcess was null for skill '{skillDef.skillNameKey}' after all checks. Defaulting to empty list.");
                    }

                    StartCoroutine(ExecuteSkillSequence(action, skillDef, actorChar, skillTargetsToProcess, animationFocusTarget, onComplete));
                }
                else
                {
                    Debug.LogError($"[ActionSequenceHandler] ExecuteAction: SkillDefinitionSO or SkillRankData is null for SKILL action type. Actor: {action.Actor.GetName()}");
                    onComplete?.Invoke();
                }
                break;

            default:
                Debug.LogWarning($"Unknown action type: {action.ActionType}");
                onComplete?.Invoke();
                break;
        }
	}

    public void ProcessAction(BattleAction action, System.Action onComplete)
    {
        // Your implementation for processing the action
        // MODIFIED: Call ExecuteAction instead of invoking onComplete directly
        if (action != null)
        {
            ExecuteAction(action, onComplete);
        }
        else
        {
            Debug.LogError("[ActionSequenceHandler] ProcessAction called with a null action.");
            onComplete?.Invoke(); // Still call onComplete if action is null to prevent hanging
        }
    }

    public bool IsSequenceRunning()
    {
        return _activeSequences > 0;
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

    // Coroutine for the main skill sequence
    // The 'targets' parameter here is the list of all characters the skill's effects will apply to.
    // The 'primaryTargetForAnim' is who the actor's animation should visually focus on.
    private IEnumerator ExecuteSkillSequence(BattleAction action, SkillDefinitionSO skillData, Character actor, List<Character> resolvedTargets, Character primaryTargetForAnim, Action onComplete)
    {
        _activeSequences++;
        // Log with resolvedTargets.Count and the primaryTargetForAnim
        Debug.Log($"<color=green>[COMBAT] Executing Skill: {skillData.skillNameKey} (Rank {action.SkillRank}) by {actor.GetName()} on {resolvedTargets.Count} target(s). Primary anim target: {(primaryTargetForAnim != null ? primaryTargetForAnim.GetName() : "None")}</color>");

        // 1. Actor Animation - uses primaryTargetForAnim for visual focus
        if (skillData.animationTrigger != AnimationTriggerName.None)
        {
            string triggerName = skillData.animationTrigger.ToString();
            Debug.Log($"[ASH] Preparing actor animation: {triggerName} for {actor.GetName()}, focusing on {primaryTargetForAnim?.GetName() ?? "area"}");
            bool actorAnimationFinished = false;
            Action actorAnimCallback = () => {
                Debug.Log($"[ASH] EXTERNAL CALLBACK: Actor ({actor.GetName()}) animation '{triggerName}' finished.");
                actorAnimationFinished = true;
            };
            actor.RegisterExternalAnimationCallback(actorAnimCallback);

            actor.PlayAnimation(skillData.animationTrigger, primaryTargetForAnim); 
            
            yield return new WaitUntil(() => actorAnimationFinished);
            Debug.Log($"[COMBAT] {actor.GetName()} confirmed completion of {triggerName} animation for skill {skillData.skillNameKey}");
        }
        else
        {
            Debug.Log($"[COMBAT] No actor animation defined (trigger is None) for skill {skillData.skillNameKey}.");
        }

        // Optional: Play skill cast sound effect here, after actor animation but before projectile/impact
        // if (skillData.skillCastSound != null)
        // {
        //     // Assuming you have an AudioManager or similar
        //     // AudioManager.Instance.PlaySound(skillData.skillCastSound);
        //     Debug.Log($"[COMBAT] Played skill cast sound for {skillData.skillNameKey}");
        // }

        // Optional: Projectile visual (if any)
        // if (skillData.projectilePrefab != null)
        // {
        //     Debug.Log($"[ASH] Spawning projectile for skill {skillData.skillNameKey}");
        //     // Instantiate projectile, set its target, etc.
        //     // This might also be a coroutine if the projectile has travel time before impact effects.
        //     // For simplicity, assuming it's quick or its travel is part of the "impact" phase.
        //     // yield return StartCoroutine(HandleProjectileSequence(skillData, actor, primaryTargetForAnimOrFirstTarget));
        // }

        // 2. Apply Skill Effects to Targets
        // This loop will now correctly wait for each target's hit animation (if any)
        // before proceeding to the next target or effect, due to changes in ApplySkillEffectToTargetWithSequence
        // List<Character> validTargets = targets; // Use the 'targets' parameter passed into this method
        // The parameter 'targets' is now 'resolvedTargets'

        foreach (var target in resolvedTargets) // Iterate over resolvedTargets
        {
            if (target == null || !target.IsAlive) 
            {
                Debug.Log($"[ASH] Target {target?.GetName() ?? "Unknown"} is null or not alive, skipping effects for skill {skillData.skillNameKey}.");
                continue;
            }

            foreach (var effectData in action.SkillRank.effects) 
            {
                yield return StartCoroutine(ApplySkillEffectToTargetWithSequence(actor, target, skillData, effectData, action.SkillRank, () => {
                    Debug.Log($"[ASH] Effect '{effectData.effectType}' applied to {target.GetName()} by {actor.GetName()} for skill {skillData.skillNameKey}.");
                }));

                if (!target.IsAlive) 
                {
                    Debug.Log($"[ASH] Target {target.GetName()} died. Breaking from applying further effects to this target from skill {skillData.skillNameKey}.");
                    break; 
                }
            }
        }

        Debug.Log($"[COMBAT] Skill sequence complete for {skillData.skillNameKey}");

        // Ensure the actor returns to Idle animation state
        if (actor != null && actor.IsAlive) // Check if actor is still valid
        {
            Debug.Log($"[ASH] Setting actor {actor.GetName()} to Idle animation after skill {skillData.skillNameKey}.");
            actor.PlayAnimation(AnimationTriggerName.Idle, null); // Assuming AnimationTriggerName.Idle exists
        }

        onComplete?.Invoke();
        _activeSequences--;
    }

    private IEnumerator ApplySkillEffectToTargetWithSequence(
        Character actor, Character target, SkillDefinitionSO skillDef, SkillEffectData effectData, SkillRankData rankData, Action onEffectAppliedToTarget)
    {
        _activeSequences++;
        Debug.Log($"[COMBAT] Processing effect '{effectData.effectType}' from skill '{skillDef.skillNameKey}' on target '{target.GetName()}' by actor '{actor.GetName()}'");

        // --- HIT ANIMATION ---
        bool playedAnimation = false;

        if (target.IsAlive)
        {
            bool targetAnimationFinished = false;

            if (effectData.effectType == SkillEffectType.Damage || effectData.effectType == SkillEffectType.DebuffStat) // Fallback to generic "Hit" for these types
            {
                Debug.Log($"[ASH] Preparing generic target 'Hit' animation for {target.GetName()} (effect type based, as skill definition does not specify a target animation).");
                target.RegisterExternalAnimationCallback(() => {
                     Debug.Log($"[ASH] EXTERNAL CALLBACK: Target ({target.GetName()}) animation 'Hit' (from TakeHit) finished.");
                    targetAnimationFinished = true;
                });
                target.TakeHit(); // Assumes TakeHit() uses the external callback system and plays a "Hit" animation
                yield return new WaitUntil(() => targetAnimationFinished);
                Debug.Log($"[COMBAT] Target ({target.GetName()}) confirmed completion of 'Hit' animation (from TakeHit).");
                
                // Ensure the target returns to Idle animation state after the hit animation
                if (target.IsAlive) // Check again, as effects haven't been applied yet but good practice
                {
                    Debug.Log($"[ASH] Setting target {target.GetName()} to Idle animation after 'Hit'.");
                    target.PlayAnimation(AnimationTriggerName.Idle, null); // Assuming AnimationTriggerName.Idle exists
                }
                playedAnimation = true;
            }
            // Add other 'else if' blocks here if other effect types should trigger specific target animations
            // and ensure they also reset to Idle if necessary.
        }
        
        if (target.IsAlive && !playedAnimation)  // MODIFIED: Was target.IsAlive()
        {
             Debug.Log($"[COMBAT] No specific hit animation played for target ({target.GetName()}) for effect '{effectData.effectType}'.");
        }
        else if (!target.IsAlive) // MODIFIED: Was !target.IsAlive()
        {
            Debug.Log($"[COMBAT] Target ({target.GetName()}) is not alive, skipping hit animation for effect '{effectData.effectType}'.");
        }
        
        // --- SFX for impact ---
        // if (effectData.impactSound != null) // SkillEffectData does not have impactSound
        // {
        //     // AudioManager.Instance.PlaySound(effectData.impactSound);
        //      Debug.Log($"[COMBAT] Played impact sound for effect '{effectData.effectType}' on {target.GetName()}");
        // }

        // --- APPLY EFFECT LOGIC (Damage, Heal, Buff, Debuff, etc.) ---
        // This part happens AFTER the hit animation (if any) has completed.
        if (target.IsAlive || effectData.effectType == SkillEffectType.Revive) // MODIFIED: Was target.IsAlive() // Example: Revive can apply to dead targets
        {
            switch (effectData.effectType)
            {
                case SkillEffectType.Damage:
                    DamageEffectResult damageResult = skillProcessor.CalculateDamageEffect(actor, target, effectData, rankData);
                    skillProcessor.ApplyAndDisplayDamage(target, damageResult); // Shows damage number
                    Debug.Log($"[COMBAT] Damage effect processed for {target.GetName()}. Crit: {damageResult.isCrit}, Final Damage: {damageResult.finalDamage}");
                    break;
                case SkillEffectType.Heal:
                    HealEffectResult healResult = skillProcessor.CalculateHealEffect(actor, target, effectData, rankData);
                    skillProcessor.ApplyAndDisplayHeal(target, healResult); // Shows heal number
                    Debug.Log($"[COMBAT] Heal effect processed for {target.GetName()}. Crit: {healResult.isCrit}, Final Heal: {healResult.finalHeal}");
                    break;
                case SkillEffectType.ApplyStatusEffect:
                    // Potency for status effect might be calculated differently or use baseValue directly
                    float potency = skillProcessor.CalculateValueWithScaling(actor, effectData.baseValue, effectData.scalingStat, effectData.scalingMultiplier);
                    target.ApplyStatusEffect(effectData, actor, potency); // Pass effectData, caster, and calculated potency
                    Debug.Log($"[COMBAT] Status effect '{effectData.statusEffectToApply?.statusNameKey}' processed for {target.GetName()}. Potency: {potency}");
                    break;
                // Add cases for Buff, Debuff, etc.
                default:
                    Debug.LogWarning($"[ASH] Effect type {effectData.effectType} not fully handled in ApplySkillEffectToTargetWithSequence.");
                    break;
            }

            // Check for death after applying the effect
            if (effectData.effectType == SkillEffectType.Damage && !target.IsAlive) // MODIFIED: Was !target.IsAlive()
            {
                Debug.Log($"[COMBAT] Target {target.GetName()} died as a result of {skillDef.skillNameKey}'s effect '{effectData.effectType}'.");
                // Optionally, play death animation (which would also need a wait)
                // bool deathAnimComplete = false;
                // target.RegisterExternalAnimationCallback(() => deathAnimComplete = true);
                // target.PlayAnimation(AnimationTriggerName.Death.ToString()); // AnimationTriggerName enum needs .ToString() if PlayAnimation expects string
                // yield return new WaitUntil(() => deathAnimComplete);
                // Debug.Log($"[COMBAT] Target {target.GetName()} death animation complete.");
            }
        }
        else
        {
            Debug.Log($"[ASH] Target {target.GetName()} is dead and effect '{effectData.effectType}' cannot apply to dead targets (unless it's Revive, etc.). Skipping effect application.");
        }

        onEffectAppliedToTarget?.Invoke();
        _activeSequences--;
    }

    private int CalculateDamage(Character attacker, Character target)
    {
        // Basic damage calculation: Attacker's AttackPower - Target's Defense
        // This should eventually use your game's damage formula, consider critical hits,
        // damage types, resistances, buffs/debuffs, etc.
        if (attacker == null || target == null)
        {
            Debug.LogError("[CalculateDamage] Attacker or Target is null.");
            return 0;
        }

        int damage = attacker.GetAttackPower() - target.GetDefense();
        damage = Mathf.Max(damage, 1); // Ensure damage is at least 1, or 0 if you prefer no damage on high defense

        // Example: Add a random variance
        // damage = Mathf.RoundToInt(damage * Random.Range(0.9f, 1.1f));
        
        Debug.Log($"[CalculateDamage] {attacker.GetName()} (AP:{attacker.GetAttackPower()}) vs {target.GetName()} (Def:{target.GetDefense()}) = Base Damage: {damage}");
        return damage;
    }

    private int CalculateMagicDamage(Character caster, Character target)
    {
        // Basic magic damage calculation: Caster's MagicPower - Target's MagicResistance
        // This should eventually use your game's magic damage formula, consider critical hits,
        // elemental types, resistances, buffs/debuffs, etc.
        if (caster == null || target == null)
        {
            Debug.LogError("[CalculateMagicDamage] Caster or Target is null.");
            return 0;
        }

        int damage = caster.GetMagicPower() - target.GetMagicResistance();
        damage = Mathf.Max(damage, 1); // Ensure damage is at least 1, or 0 if you prefer

        // Example: Add a random variance
        // damage = Mathf.RoundToInt(damage * Random.Range(0.9f, 1.1f));

        Debug.Log($"[CalculateMagicDamage] {caster.GetName()} (MP:{caster.GetMagicPower()}) vs {target.GetName()} (MR:{target.GetMagicResistance()}) = Base Magic Damage: {damage}");
        return damage;
    }
}