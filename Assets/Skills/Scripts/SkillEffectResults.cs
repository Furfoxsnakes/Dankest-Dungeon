// Add these structs, or place them in a separate file like "SkillEffectResults.cs"
public struct DamageEffectResult
{
    public bool success;
    public int finalDamage;
    public bool isCrit;
    public ElementType elementType; // Added
}

public struct HealEffectResult
{
    public bool success;
    public int finalHeal;
    public bool isCrit;
    public ElementType elementType; // Added (e.g. for "Holy" heal vs "Nature" heal)
}

public struct StatModEffectResult
{
	public StatType statToModify;
	public float modValue;
	public int duration;
	public bool isBuff;
	public bool success;
}

public struct StatusEffectApplicationResult
{
    // public StatusEffectSO statusEffectToApply;
    public string statusEffectName; // Example, you'll likely replace this with a more robust system
    public int duration;
    public bool success;
    public ElementType elementType; // This is already here, ensure it's populated
    public float potency; // Potentially add potency if status effects scale (e.g. DoT damage per tick)
}