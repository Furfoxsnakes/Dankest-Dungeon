using System;

[Serializable]
public class Stats
{
    public int health;
    public int attackPower;
    public int defense;

    public Stats(int health, int attackPower, int defense)
    {
        this.health = health;
        this.attackPower = attackPower;
        this.defense = defense;
    }

    public void TakeDamage(int damage)
    {
        int damageTaken = Math.Max(damage - defense, 0);
        health -= damageTaken;
        if (health < 0) health = 0;
    }

    public void Heal(int amount)
    {
        health += amount;
    }
}