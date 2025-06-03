using UnityEngine;

public class Hero : Character
{
    [SerializeField] private GameObject deathEffect;
    
    public override void Die()
    {
        // Call base implementation first
        base.Die();
        
        // Other hero-specific death behavior...
    }
}