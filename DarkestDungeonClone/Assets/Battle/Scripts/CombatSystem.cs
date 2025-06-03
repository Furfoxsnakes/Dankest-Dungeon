using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public void PerformAttack(Character attacker, Character target)
    {
        // Calculate damage based on attacker's stats and target's defense
        int damage = CalculateDamage(attacker, target);
        ApplyDamage(target, damage);
    }

    private int CalculateDamage(Character attacker, Character target)
    {
        // Example damage calculation logic
        int baseDamage = attacker.stats.attackPower;
        int damageReduction = target.stats.defense;
        return Mathf.Max(baseDamage - damageReduction, 0);
    }

    private void ApplyDamage(Character target, int damage)
    {
        target.stats.health -= damage;
        if (target.stats.health <= 0)
        {
            // Handle character defeat
            HandleDefeat(target);
        }
    }

    private void HandleDefeat(Character target)
    {
        // Logic for handling character defeat
        Debug.Log(target.name + " has been defeated!");
    }
}