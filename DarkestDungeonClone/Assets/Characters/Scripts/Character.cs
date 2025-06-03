using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public int health;
    public int attackPower;
    public int defense;

    public virtual void Attack(Character target)
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

    protected virtual void Die()
    {
        // Handle character death (e.g., play animation, remove from battle)
        Debug.Log(characterName + " has died.");
    }
}