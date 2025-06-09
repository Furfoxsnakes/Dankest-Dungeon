using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DankestDungeon.Skills;
using DankestDungeon.Characters; // For Character
using System.Linq; // For FirstOrDefault

public class ActionSequenceHandler : MonoBehaviour
{
    // Dependencies that will be set by CombatSystem
    private CombatSystem combatSystem;
    private SkillEffectProcessor skillProcessor;
    private TargetingSystem targetingSystem;
    private BattleUI battleUI; // If needed, can also be passed or found

    // You might still have [SerializeField] for some dependencies if they are also set in Inspector
    // For example, if BattleUI is always set via Inspector:
    // [SerializeField] private BattleUI battleUI;


    // Add this Initialize method
    public void Initialize(CombatSystem cs, SkillEffectProcessor sp, TargetingSystem ts)
    {
        this.combatSystem = cs;
        this.skillProcessor = sp;
        this.targetingSystem = ts;

        // Optionally, get BattleUI if it's a dependency for ActionSequenceHandler as well
        // If CombatSystem has a public getter for BattleUI:
        // this.battleUI = cs.GetBattleUI(); // Assuming CombatSystem has GetBattleUI()
        // Or if it's found globally:
        // this.battleUI = FindObjectOfType<BattleUI>();

        if (this.combatSystem == null) Debug.LogError("[ActionSequenceHandler] CombatSystem not initialized.");
        if (this.skillProcessor == null) Debug.LogError("[ActionSequenceHandler] SkillEffectProcessor not initialized.");
        if (this.targetingSystem == null) Debug.LogError("[ActionSequenceHandler] TargetingSystem not initialized.");
        
        Debug.Log("[ActionSequenceHandler] Initialized successfully.");
    }

    public void ProcessAction(BattleAction action, Action onActionComplete)
    {
        if (action == null || action.Actor == null)
        {
            Debug.LogError("[ActionSequenceHandler] ProcessAction called with null action or actor.");
            onActionComplete?.Invoke();
            return;
        }

        if (skillProcessor == null || targetingSystem == null || combatSystem == null)
        {
            Debug.LogError("[ActionSequenceHandler] Dependencies not set. Cannot process action.");
            onActionComplete?.Invoke();
            return;
        }

        StartCoroutine(ProcessActionCoroutine(action, onActionComplete));
    }

    private bool _animationEventReceived = false; // Flag to be set by the animation complete callback

    private IEnumerator ProcessActionCoroutine(BattleAction action, Action onActionComplete)
    {
        Character caster = action.Actor;
        List<Character> finalTargetsToProcess = new List<Character>();

        // --- Target Resolution (remains the same) ---
        if (action.ActionType == ActionType.Skill && action.UsedSkill != null)
        {
            SkillDefinitionSO skillDef = action.UsedSkill;
            SkillRankData rankData = caster.Skills.GetSkillRankData(skillDef);

            if (rankData == null)
            {
                Debug.LogError($"[ASH Coroutine] Could not find rank data for skill {skillDef.skillNameKey} on caster {caster.GetName()}.");
                onActionComplete?.Invoke();
                yield break;
            }

            if (skillDef.targetType == SkillTargetType.SingleEnemy || skillDef.targetType == SkillTargetType.SingleAlly)
            {
                if (action.Target != null && action.Target.IsAlive)
                {
                    finalTargetsToProcess.Add(action.Target);
                }
            }
            else 
            {
                List<Character> resolvedSystemTargets = targetingSystem.GetTargetsForSkill(caster, skillDef, combatSystem.GetPlayerTeamCharacters(), combatSystem.GetEnemyTeamCharacters());
                if (resolvedSystemTargets != null)
                {
                    finalTargetsToProcess.AddRange(resolvedSystemTargets.Where(t => t != null && t.IsAlive));
                }
            }

            if (!finalTargetsToProcess.Any())
            {
                Debug.LogWarning($"[ASH Coroutine] Skill {skillDef.skillNameKey} by {caster.GetName()} found no valid targets to process.");
                onActionComplete?.Invoke();
                yield break;
            }
            // --- End of Target Resolution ---

            // --- Skill Animation & Effect Sequence ---
            Debug.Log($"[ASH Coroutine] {caster.GetName()} starting skill sequence for {skillDef.skillNameKey}.");

            // 1. Caster's attack/cast animation
            _animationEventReceived = false;
            caster.RegisterExternalAnimationCallback(() => // Use new method
            {
                Debug.Log($"[ASH - Caster CB] External animation event received for {caster.GetName()}");
                _animationEventReceived = true;
            });
            AnimationType casterAnimType = skillDef.animationType;
            caster.PlayAnimation(casterAnimType, finalTargetsToProcess.FirstOrDefault());
            yield return new WaitUntil(() => _animationEventReceived);
            Debug.Log($"[ASH Coroutine] {caster.GetName()}'s {casterAnimType} animation complete (ASH perspective).");

            // 2. Process each target
            foreach (Character target in finalTargetsToProcess)
            {
                if (target == null || !target.IsAlive) continue;

                // 2a. Target's hit animation
                _animationEventReceived = false;
                target.RegisterExternalAnimationCallback(() => // Use new method
                {
                    Debug.Log($"[ASH - Target CB] External animation event received for {target.GetName()}");
                    _animationEventReceived = true;
                });
                target.TakeHit();
                yield return new WaitUntil(() => _animationEventReceived);
                Debug.Log($"[ASH Coroutine] {target.GetName()}'s hit animation complete (ASH perspective).");

                // 2b. Apply skill effects to this target
                Debug.Log($"[ASH Coroutine] Applying skill '{skillDef.skillNameKey}' from {caster.GetName()} to {target.GetName()}");
                foreach (SkillEffectData effectData in rankData.effects)
                {
                    switch (effectData.effectType)
                    {
                        case SkillEffectType.Damage:
                            DamageEffectResult damageResult = skillProcessor.CalculateDamageEffect(caster, target, effectData, rankData);
                            if (damageResult.success) skillProcessor.ApplyAndDisplayDamage(target, damageResult);
                            break;
                        case SkillEffectType.Heal:
                            HealEffectResult healResult = skillProcessor.CalculateHealEffect(caster, target, effectData, rankData);
                            if (healResult.success) skillProcessor.ApplyAndDisplayHeal(target, healResult);
                            break;
                        case SkillEffectType.ApplyStatusEffect:
                            if (effectData.statusEffectToApply != null)
                            {
                                StatusEffectApplicationResult statusAppResult = skillProcessor.CalculateStatusEffectApplication(caster, target, effectData, rankData);
                                if (statusAppResult.success) target.ApplyStatusEffect(effectData, caster, statusAppResult.potency);
                            }
                            break;
                    }
                }

                // 2c. (Optional) Wait for target to return to idle or a brief recovery pause
                // If your HitState automatically transitions to Idle and IdleState calls OnAnimationComplete:
                // _animationEventReceived = false;
                // target.RegisterAnimationCallback(() => _animationEventReceived = true);
                // // No explicit PlayAnimation(Idle) needed if HitState handles transition and event
                // yield return new WaitUntil(() => _animationEventReceived);
                // Debug.Log($"[ASH Coroutine] {target.GetName()} returned to idle.");
                // OR a small fixed delay if preferred after effects
                yield return new WaitForSeconds(0.1f); // Small delay after effects on one target before next
            }
        }
        else if (action.ActionType == ActionType.Attack) // Basic Attack
        {
            Debug.Log($"[ASH Coroutine] {caster.GetName()} starting basic attack sequence.");

            // 1. Caster's basic attack animation
            _animationEventReceived = false;
            caster.RegisterExternalAnimationCallback(() => // Use new method
            {
                Debug.Log($"[ASH - Caster BasicAttack CB] External animation event received for {caster.GetName()}");
                _animationEventReceived = true;
            });
            caster.Attack(action.Target); // Play basic attack animation
            yield return new WaitUntil(() => _animationEventReceived);
            Debug.Log($"[ASH Coroutine] {caster.GetName()}'s basic attack animation complete (ASH perspective).");

            if (action.Target != null && action.Target.IsAlive)
            {
                // 2. Target's hit animation
                _animationEventReceived = false;
                action.Target.RegisterExternalAnimationCallback(() => // Use new method
                {
                    Debug.Log($"[ASH - Target BasicHit CB] External animation event received for {action.Target.GetName()}");
                    _animationEventReceived = true;
                });
                action.Target.TakeHit();
                yield return new WaitUntil(() => _animationEventReceived);
                Debug.Log($"[ASH Coroutine] {action.Target.GetName()}'s hit animation complete (ASH perspective).");

                // 3. Apply basic attack damage (example - you'll need actual logic)
                // skillProcessor.ApplyBasicAttack(caster, action.Target);
                Debug.Log($"[ASH Coroutine] Basic attack by {caster.GetName()} hits {action.Target.GetName()}. (Damage application logic needed)");
                
                // 4. (Optional) Wait for target to return to idle
                // _animationEventReceived = false;
                // action.Target.RegisterAnimationCallback(() => _animationEventReceived = true);
                // yield return new WaitUntil(() => _animationEventReceived);
                // Debug.Log($"[ASH Coroutine] {action.Target.GetName()} returned to idle after basic attack.");
            }
        }
        // ... other action types ...

        // Ensure caster returns to idle
        Debug.Log($"[ASH Coroutine] Ensuring {caster.GetName()} returns to idle (ASH perspective).");
        _animationEventReceived = false;
        caster.RegisterExternalAnimationCallback(() => // Use new method
        {
            Debug.Log($"[ASH - Caster Idle CB] External animation event received for {caster.GetName()}");
            _animationEventReceived = true;
        });
        caster.PlayAnimation(AnimationType.Idle); // This should trigger IdleState, which might have its own internal callback
        yield return new WaitUntil(() => _animationEventReceived);
        Debug.Log($"[ASH Coroutine] {caster.GetName()} confirmed idle (ASH perspective).");


        Debug.Log("[ASH Coroutine] Action processing fully complete.");
        onActionComplete?.Invoke();
    }
}