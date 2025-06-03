using UnityEngine;

public class Enemy : Character
{
    public int attackPower;
    public int defense;
    public int health;

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
        // Handle enemy death (e.g., play animation, remove from battle)
    }
}