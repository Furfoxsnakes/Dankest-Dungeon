using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DefensiveAI", menuName = "Dankest Dungeon/AI/Defensive")]
public class DefensiveAI : AIBehaviorSO
{
    [Range(0, 100)]
    [SerializeField] private int defendChance = 30;
    
    public override BattleAction DecideAction(Character self, List<Character> allies, List<Character> enemies)
    {
        // First check health percentage to see if we should defend
        float healthPercentage = (float)self.CurrentHealth / (float)self.Stats.maxHealth;
        
        if (healthPercentage < 0.3f && Random.Range(0, 100) < defendChance)
        {
            // Low health - chance to defend
            return new BattleAction(self, self, ActionType.Defend);
        }
        
        // Otherwise, find a target and attack
        var aliveTargets = enemies.FindAll(t => t.IsAlive());
        if (aliveTargets.Count == 0) return null;
        
        // Just pick a random target
        Character target = aliveTargets[Random.Range(0, aliveTargets.Count)];
        return new BattleAction(self, target, ActionType.Attack);
    }
}