using UnityEngine;
using System;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    [SerializeField] private AIBehaviorSO behavior;
    
    private Character character;
    
    void Awake()
    {
        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("AIController requires a Character component");
        }
    }
    
    public BattleAction DecideAction(List<Character> allies, List<Character> enemies)
    {
        if (behavior == null)
        {
            Debug.LogWarning($"No AI behavior assigned for {character.GetName()}");
            return GetDefaultAction(enemies);
        }
        
        return behavior.DecideAction(character, allies, enemies);
    }
    
    private BattleAction GetDefaultAction(List<Character> possibleTargets)
    {
        // Simple fallback AI - target random enemy with basic attack
        var aliveTargets = possibleTargets.FindAll(t => t.IsAlive());
        if (aliveTargets.Count == 0) return null;
        
        Character target = aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
        return new BattleAction(character, target, ActionType.Attack);
    }
}