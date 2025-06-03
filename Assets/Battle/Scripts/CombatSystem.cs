using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    // Store references to character lists for damage calculations and validation
    private List<Character> playerCharacters;
    private List<Character> enemyCharacters;
    
    public void Initialize(List<Character> players, List<Character> enemies)
    {
        playerCharacters = players;
        enemyCharacters = enemies;
        
        Debug.Log($"[COMBAT] Combat system initialized with {players.Count} players and {enemies.Count} enemies");
        
        // Validate that all characters have the required components
        foreach (var character in players)
        {
            ValidateCharacter(character, "Player");
        }
        
        foreach (var character in enemies)
        {
            ValidateCharacter(character, "Enemy");
        }
    }
    
    private void ValidateCharacter(Character character, string type)
    {
        if (character == null)
        {
            Debug.LogError($"[COMBAT] Null {type} character found during initialization!");
            return;
        }
        
        if (!character.IsAlive())
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
        if (!target.IsAlive())
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
        if (!target.IsAlive())
        {
            Debug.Log($"[COMBAT] Target ({target.GetName()}) died during hit animation. Skipping further damage application.");
            Debug.Log($"[COMBAT] Attack sequence complete (target died during hit).");
            onComplete?.Invoke();
            yield break;
        }

        int damage = CalculateDamage(attacker, target);
        Debug.Log($"[COMBAT] Applying {damage} damage to {target.GetName()}.");
        target.ReceiveDamage(damage); // Use the method that only applies health change

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
            if (!target.IsAlive())
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
            if (target.IsAlive())
            {
                int damage = CalculateMagicDamage(caster, target);
                Debug.Log($"[COMBAT] Applying {damage} magic damage to {target.GetName()}.");
                target.ReceiveDamage(damage); // Use the method that only applies health change
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
            if (!target.IsAlive())
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
    
    private int CalculateDamage(Character attacker, Character target)
    {
        int attackPower = attacker.GetAttackPower();
        int defense = target.GetDefense();
        return Mathf.Max(1, attackPower - defense);
    }
    
    private int CalculateMagicDamage(Character caster, Character target)
    {
        int magicPower = caster.GetMagicPower();
        int magicResistance = target.GetMagicResistance();
        return Mathf.Max(1, magicPower - magicResistance);
    }
}