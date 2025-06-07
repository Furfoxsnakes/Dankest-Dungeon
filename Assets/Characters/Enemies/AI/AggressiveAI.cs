using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AggressiveAI", menuName = "Dankest Dungeon/AI/Aggressive")]
public class AggressiveAI : AIBehaviorSO
{
    [Tooltip("Chance to use magic instead of basic attack (0-100)")]
    [Range(0, 100)]
    [SerializeField] private int magicChance = 30;
    
    [SerializeField] private string[] spells = { "Fireball", "Lightning" };
    
    public override BattleAction DecideAction(Character self, List<Character> allies, List<Character> enemies)
    {
        // Find valid targets
        var aliveTargets = enemies.FindAll(t => t.IsAlive);
        if (aliveTargets.Count == 0) return null;
        
        // Target selection - focus on lowest health enemy
        Character target = FindLowestHealthTarget(aliveTargets);
        
        // Action selection - chance to use magic
        if (Random.Range(0, 100) < magicChance && self.GetMagicPower() > 0)
        {
            string spell = spells[Random.Range(0, spells.Length)];
            return new BattleAction(self, target, ActionType.Magic);
        }
        else
        {
            return new BattleAction(self, target, ActionType.Attack);
        }
    }
    
    private Character FindLowestHealthTarget(List<Character> targets)
    {
        Character lowestHealthTarget = targets[0];
        int lowestHealth = lowestHealthTarget.CurrentHealth;
        
        foreach (var target in targets)
        {
            if (target.CurrentHealth < lowestHealth)
            {
                lowestHealth = target.CurrentHealth;
                lowestHealthTarget = target;
            }
        }
        
        return lowestHealthTarget;
    }
}