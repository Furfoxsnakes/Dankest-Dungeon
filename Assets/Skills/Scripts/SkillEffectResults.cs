// Add these structs, or place them in a separate file like "SkillEffectResults.cs"
public struct DamageEffectResult
{
	public int finalDamage;
	public bool isCrit;
	public bool success; // True if calculation was possible (e.g., target valid)
}

public struct HealEffectResult
{
	public int finalHeal;
	public bool isCrit;
	public bool success;
}

public struct StatModEffectResult
{
	public StatType statToModify;
	public float modValue;
	public int duration;
	public bool isBuff;
	public bool success;
}

public struct StatusEffectApplicationResult // Placeholder for now
{
	// public StatusEffectSO statusEffectToApply; // If you have StatusEffect ScriptableObjects
	public string statusEffectName; // Example
	public int duration;
	public bool success;
}