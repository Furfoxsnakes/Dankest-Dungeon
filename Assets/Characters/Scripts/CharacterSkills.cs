using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using DankestDungeon.Skills; // Assuming SkillDefinitionSO is here

[System.Serializable]
public class CharacterSkills
{
    // === SERIALIZABLE SKILL SYSTEM ===
    [System.Serializable]
    public class SkillEntry
    {
        public SkillDefinitionSO skill;
        [Range(1, 5)]
        public int rank = 1;

        public SkillEntry() { }
        public SkillEntry(SkillDefinitionSO skill, int rank)
        {
            this.skill = skill;
            this.rank = rank;
        }
    }

    [BoxGroup("Learned Skills List", CenterLabel = true)]
    [PropertyOrder(10)]
    [ListDrawerSettings(
        DraggableItems = false,
        HideAddButton = false,
        HideRemoveButton = false,
        ShowItemCount = true,
        CustomAddFunction = "AddSkillEntryFromUI" // This method is now within CharacterSkills
    )]
    [SerializeField]
    private List<SkillEntry> serializedSkills = new List<SkillEntry>();

    // Runtime dictionary for fast lookups - populated from serializedSkills
    private Dictionary<SkillDefinitionSO, int> learnedSkills = new Dictionary<SkillDefinitionSO, int>();
    private string characterName = "Character"; // For logging

    public void Initialize(string ownerName)
    {
        this.characterName = ownerName;
        SyncSkillsFromSerializedList();
    }

    private void AddSkillEntryFromUI()
    {
        serializedSkills.Add(new SkillEntry());
    }

    [BoxGroup("Skill Management Actions", CenterLabel = true)]
    [PropertyOrder(20)]
    [InfoBox("Skills are stored in the list above. Use the buttons below for additional management options.", InfoMessageType.Info)]
    [Button("Sync Skills to Dictionary", ButtonSizes.Medium), GUIColor(0.6f, 0.8f, 1f)]
    public void SyncSkillsFromSerializedList()
    {
        learnedSkills.Clear();
        if (serializedSkills == null) serializedSkills = new List<SkillEntry>();

        serializedSkills.RemoveAll(entry => entry.skill == null);

        for (int i = 0; i < serializedSkills.Count; i++)
        {
            var entry = serializedSkills[i];
            if (entry.skill == null)
            {
                Debug.LogWarning($"Found a null skill entry at index {i} for {characterName} during sync. Removing.");
                serializedSkills.RemoveAt(i);
                i--;
                continue;
            }

            int maxRank = entry.skill.ranks?.Count ?? 0;
            if (maxRank == 0)
            {
                Debug.LogWarning($"Skill {entry.skill.skillNameKey} for {characterName} has no ranks defined. Removing.");
                serializedSkills.RemoveAt(i);
                i--;
                continue;
            }

            if (entry.rank < 1 || entry.rank > maxRank)
            {
                Debug.LogWarning($"Invalid rank {entry.rank} for skill {entry.skill.skillNameKey} on {characterName}. Clamping to valid range [1-{maxRank}].");
                entry.rank = Mathf.Clamp(entry.rank, 1, maxRank);
            }

            if (learnedSkills.ContainsKey(entry.skill))
            {
                if (entry.rank > learnedSkills[entry.skill])
                {
                    learnedSkills[entry.skill] = entry.rank;
                }
            }
            else
            {
                learnedSkills[entry.skill] = entry.rank;
            }
        }

        var uniqueSkills = new Dictionary<SkillDefinitionSO, SkillEntry>();
        foreach (var entry in serializedSkills)
        {
            if (entry.skill == null) continue;
            if (uniqueSkills.ContainsKey(entry.skill))
            {
                if (entry.rank > uniqueSkills[entry.skill].rank)
                {
                    uniqueSkills[entry.skill] = entry;
                }
            }
            else
            {
                uniqueSkills[entry.skill] = entry;
            }
        }
        serializedSkills = uniqueSkills.Values.ToList();
        foreach(var s_entry in serializedSkills)
        {
            if(learnedSkills.TryGetValue(s_entry.skill, out int dictRank))
            {
                s_entry.rank = dictRank;
            }
        }
        Debug.Log($"Synced {learnedSkills.Count} skills to dictionary for {characterName}");
    }

    [BoxGroup("Skill Management Actions")]
    [PropertyOrder(21)]
    [Button("Clear All Skills", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
    public void ClearAllSkills()
    {
        if (serializedSkills.Count == 0 && learnedSkills.Count == 0)
        {
            Debug.Log($"{characterName} has no skills to clear.");
            return;
        }
        int skillCount = serializedSkills.Count > 0 ? serializedSkills.Count : learnedSkills.Count;
        serializedSkills.Clear();
        learnedSkills.Clear();
        Debug.Log($"Cleared {skillCount} skills from {characterName}");
    }

    [BoxGroup("Quick Skill Modification", CenterLabel = true)]
    [PropertyOrder(30)]
    [Title("Quick Add Skill", titleAlignment: TitleAlignments.Centered)]
    [SerializeField, HideLabel]
    [InfoBox("Select a skill to add and specify the rank.", InfoMessageType.None)]
    private SkillDefinitionSO skillToAdd;

    [BoxGroup("Quick Skill Modification")]
    [PropertyOrder(31)]
    [SerializeField, Range(1, 5), SuffixLabel("Rank")]
    [ShowIf("@skillToAdd != null")]
    private int rankToAdd = 1;

    [BoxGroup("Quick Skill Modification")]
    [PropertyOrder(32)]
    [Button("Add/Upgrade Skill", ButtonSizes.Large), GUIColor(0.6f, 1f, 0.6f)]
    [EnableIf("@skillToAdd != null")]
    private void AddSkillFromEditor()
    {
        if (skillToAdd == null)
        {
            Debug.LogWarning("No skill selected to add.");
            return;
        }
        LearnSkill(skillToAdd, rankToAdd);
        skillToAdd = null;
        rankToAdd = 1;
    }

    [BoxGroup("Quick Skill Modification")]
    [PropertyOrder(33)]
    [Title("Remove Skill", titleAlignment: TitleAlignments.Centered)]
    [ValueDropdown("GetLearnedSkillDefinitionsForEditor")]
    [SerializeField, HideLabel]
    [InfoBox("Select a skill to remove from this character.", InfoMessageType.None)]
    private SkillDefinitionSO skillToRemove;

    [BoxGroup("Quick Skill Modification")]
    [PropertyOrder(34)]
    [Button("Remove Selected Skill", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.6f)]
    [EnableIf("@skillToRemove != null")]
    private void RemoveSkillFromEditor()
    {
        if (skillToRemove == null)
        {
            Debug.LogWarning("No skill selected to remove.");
            return;
        }
        ForgetSkill(skillToRemove);
        skillToRemove = null;
    }
    
    // Helper for ValueDropdown
    private IEnumerable<SkillDefinitionSO> GetLearnedSkillDefinitionsForEditor()
    {
        return learnedSkills.Keys.ToList();
    }

    public IReadOnlyDictionary<SkillDefinitionSO, int> LearnedSkills => learnedSkills;

    public void LearnSkill(SkillDefinitionSO skill, int rank = 1)
    {
        if (skill == null)
        {
            Debug.LogError($"Attempted to learn a null skill on {characterName}");
            return;
        }
        int maxRank = skill.ranks?.Count ?? 0;
        if (maxRank == 0)
        {
            Debug.LogError($"Skill {skill.skillNameKey} has no ranks defined for {characterName}. Cannot add.");
            return;
        }
        rank = Mathf.Clamp(rank, 1, maxRank);

        if (learnedSkills.ContainsKey(skill))
        {
            if (rank > learnedSkills[skill])
            {
                learnedSkills[skill] = rank;
                Debug.Log($"{characterName} upgraded skill {skill.skillNameKey} to rank {rank}");
            }
            else
            {
                Debug.Log($"{characterName} already knows {skill.skillNameKey} at rank {learnedSkills[skill]} or higher (attempted {rank}).");
                return;
            }
        }
        else
        {
            learnedSkills.Add(skill, rank);
            Debug.Log($"{characterName} learned new skill {skill.skillNameKey} at rank {rank}");
        }

        var existingEntry = serializedSkills.FirstOrDefault(e => e.skill == skill);
        if (existingEntry != null)
        {
            existingEntry.rank = rank;
        }
        else
        {
            serializedSkills.Add(new SkillEntry(skill, rank));
        }
    }

    public void ForgetSkill(SkillDefinitionSO skill)
    {
        if (skill == null) return;
        if (learnedSkills.Remove(skill))
        {
            Debug.Log($"{characterName} forgot skill {skill.skillNameKey}");
            serializedSkills.RemoveAll(entry => entry.skill == skill);
        }
    }

    public bool KnowsSkill(SkillDefinitionSO skill)
    {
        return skill != null && learnedSkills.ContainsKey(skill);
    }

    public int GetSkillRank(SkillDefinitionSO skill)
    {
        return KnowsSkill(skill) ? learnedSkills[skill] : 0;
    }

    public SkillRankData GetSkillRankData(SkillDefinitionSO skill)
    {
        if (KnowsSkill(skill))
        {
            int currentRank = learnedSkills[skill];
            if (currentRank > 0 && skill.ranks != null && currentRank <= skill.ranks.Count)
            {
                return skill.ranks[currentRank - 1];
            }
            Debug.LogError($"Skill {skill.skillNameKey} for {characterName} has rank {currentRank}, but data is out of bounds (max: {skill.ranks?.Count ?? 0}).");
        }
        return null;
    }

    public List<SkillDefinitionSO> GetAvailableSkills()
    {
        return learnedSkills.Keys.ToList();
    }

    // Example methods that might remain in Character.cs or be adapted
    // For instance, LoadDefaultSkills might take a CharacterStatsSO that defines defaults
    public void ExportSkillsToString(System.Text.StringBuilder sb)
    {
        if (learnedSkills.Count == 0)
        {
            sb.AppendLine($"{characterName} has no skills to export.");
            return;
        }
        sb.AppendLine($"=== Skills for {characterName} ===");
        foreach (var kvp in learnedSkills)
        {
            sb.AppendLine($"- {kvp.Key.skillNameKey} (Rank {kvp.Value})");
        }
        sb.AppendLine("========================");
    }
}