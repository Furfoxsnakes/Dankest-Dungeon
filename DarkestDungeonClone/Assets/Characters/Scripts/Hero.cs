using UnityEngine;

public class Hero : Character
{
    public int level;
    public int experience;
    public int health;
    public int attackPower;
    public int defense;

    public void LevelUp()
    {
        level++;
        experience = 0;
        // Increase stats on level up
        health += 10;
        attackPower += 2;
        defense += 1;
    }

    public void Attack(Character target)
    {
        int damage = Mathf.Max(0, attackPower - target.defense);
        target.TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Handle hero death (e.g., trigger animations, remove from battle)
    }
}