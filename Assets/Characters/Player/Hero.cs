using UnityEngine;
using System.Collections.Generic; // Required for IReadOnlyDictionary and List
using DankestDungeon.Skills;    // Required for SkillDefinitionSO, SkillRankData
using Sirenix.OdinInspector;    // Required for Odin attributes

public class Hero : Character
{
    [SerializeField] private GameObject deathEffect;

    // === MODULES ===
    [BoxGroup("Hero Modules", CenterLabel = true)]
    [PropertyOrder(5)] // Ensure it appears after base Character fields if any are shown
    [SerializeField, InlineProperty, HideLabel]
    private CharacterSkills characterSkills = new CharacterSkills();
    public CharacterSkills Skills => characterSkills; // Public accessor

    // === ODIN INSPECTOR QUICK ACTIONS ===
    [BoxGroup("Hero Skill Actions", CenterLabel = true)]
    [PropertyOrder(100)]
    [Button("Load Default Skills (Hero-Specific)"), GUIColor(0.8f, 0.8f, 1f)]
    private void LoadDefaultSkills()
    {
        // This method would typically involve characterStats or a specific config
        // to know which skills are "default" for this hero class.
        // For example:
        // if (Stats.defaultSkills != null) { // Assuming CharacterStats has a list of default skills
        //     foreach(var skillInfo in Stats.defaultSkills) { // Assuming skillInfo has skill and rank
        //         Skills.LearnSkill(skillInfo.skill, skillInfo.rank);
        //     }
        // }
        Debug.Log($"LoadDefaultSkills called for {GetName()}. Implement hero-specific default skills here. Skills can be added via Hero.Skills.LearnSkill().");
        // Example: if you have a specific default skill for all heroes for testing:
        // SkillDefinitionSO testSkill = Resources.Load<SkillDefinitionSO>("Path/To/Your/TestSkillSO");
        // if (testSkill != null) Skills.LearnSkill(testSkill, 1);
    }

    [BoxGroup("Hero Skill Actions")]
    [PropertyOrder(101)]
    [Button("Export Skills (Console)"), GUIColor(0.8f, 1f, 0.8f)]
    private void ExportSkillsToConsole()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Skills.ExportSkillsToString(sb); // Delegate to CharacterSkills
        Debug.Log(sb.ToString());
    }

    // === PUBLIC SKILL ACCESSORS (delegating to CharacterSkills) ===
    public IReadOnlyDictionary<SkillDefinitionSO, int> LearnedSkills => Skills.LearnedSkills;
    public void LearnSkill(SkillDefinitionSO skill, int rank = 1) => Skills.LearnSkill(skill, rank);
    public void ForgetSkill(SkillDefinitionSO skill) => Skills.ForgetSkill(skill);
    public bool KnowsSkill(SkillDefinitionSO skill) => Skills.KnowsSkill(skill);
    public int GetSkillRank(SkillDefinitionSO skill) => Skills.GetSkillRank(skill);
    public SkillRankData GetSkillRankData(SkillDefinitionSO skill) => Skills.GetSkillRankData(skill);
    public List<SkillDefinitionSO> GetAvailableSkills() => Skills.GetAvailableSkills();

    protected override void Awake()
    {
        base.Awake(); // Call base Character.Awake() first
        Skills.Initialize(GetName()); // Initialize CharacterSkills for the Hero
    }
    
    public override void Die()
    {
        // Call base implementation first
        base.Die();
        
        // Play death effect if assigned
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        // Other hero-specific death behavior...
        Debug.Log($"Hero {GetName()} has fallen!");
    }
}