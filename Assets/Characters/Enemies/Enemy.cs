using UnityEngine;
using System.Collections.Generic; // Add this for List<T>

public class Enemy : Character
{
    [SerializeField] private int experienceValue = 10;
    [SerializeField] private GameObject lootPrefab;
    
    private AIController aiController;
    
    protected override void Awake()
    {
        base.Awake();

        aiController = GetComponent<AIController>();
        
        // Add AI controller if not present
        if (aiController == null)
        {
            aiController = gameObject.AddComponent<AIController>();
        }
    }
    
    public BattleAction DecideAction(List<Character> allies, List<Character> enemies)
    {
        if (aiController != null)
        {
            return aiController.DecideAction(allies, enemies);
        }
        
        // Fallback if no AI controller
        return GetDefaultAction(enemies);
    }
    
    private BattleAction GetDefaultAction(List<Character> possibleTargets)
    {
        // Simple fallback AI - target random enemy with basic attack
        var aliveTargets = possibleTargets.FindAll(t => t.IsAlive);
        if (aliveTargets.Count == 0) return null;
        
        Character target = aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
        return new BattleAction(this, target, ActionType.Attack);
    }
    
    public override void Die()
    {
        // Call base implementation first
        base.Die();
        
        // Enemy-specific death behavior
        Debug.Log($"Enemy {GetName()} has been defeated! +{experienceValue} XP");
        
        // Drop loot
        if (lootPrefab != null && Random.value > 0.5f)
        {
            Instantiate(lootPrefab, transform.position, Quaternion.identity);
        }
    }
}