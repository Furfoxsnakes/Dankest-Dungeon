using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using DankestDungeon.Skills;
using System.Linq; 
using System; // Required for GUID

namespace DankestDungeon.Skills
{
    [CreateAssetMenu(fileName = "NewSkillDefinition", menuName = "Dankest Dungeon/Skills/Skill Definition")]
    public class SkillDefinitionSO : ScriptableObject
    {
        [BoxGroup("General Info", CenterLabel = true)]
        [PropertyOrder(-10)]
        [InfoBox("A unique identifier for this skill. Auto-generated if empty.", InfoMessageType.None)]
        [ValidateInput("IsSkillIdValid", "Skill ID cannot be empty and should be unique.")]
        [DisplayAsString] // Makes it look like text but not easily editable, good for generated IDs
        public string skillId;

        [BoxGroup("General Info")]
        [Button("Generate New Skill ID", ButtonSizes.Small)]
        [PropertyOrder(-9)]
        private void GenerateNewSkillId()
        {
            skillId = Guid.NewGuid().ToString();
        }

        [BoxGroup("General Info")]
        [PropertyOrder(-8)]
        [Required("Skill Name Key cannot be empty.")]
        [InfoBox("Localization key for the skill's display name.", InfoMessageType.None)]
        public string skillNameKey;
        
        [BoxGroup("General Info")]
        [PropertyOrder(-7)]
        [TextArea(2,4)]
        [InfoBox("This is the in-editor preview/notes for the general skill description. Rank-specific details are in ranks.", InfoMessageType.None)]
        public string editorBaseDescriptionPreview;

        [BoxGroup("Visuals & Animation", CenterLabel = true)]
        [PreviewField(75, ObjectFieldAlignment.Center)]
        [Required]
        public Sprite icon;

        [BoxGroup("Visuals & Animation")]
        [Tooltip("Animation type to play when this skill is used.")]
        [EnumToggleButtons] // Makes it look nice in the inspector
        public AnimationType animationType = AnimationType.Attack; // Default to Attack

        [BoxGroup("Targeting & Usage", CenterLabel = true)]
        [EnumToggleButtons]
        public SkillTargetType targetType;

        [BoxGroup("Targeting & Usage")]
        [Tooltip("Which party positions can this skill be used FROM? (1-4)")]
        [ListDrawerSettings(ShowFoldout = false, AddCopiesLastElement = true)]
        public List<int> launchPositions = new List<int>() {1,2,3,4}; // Default to all

        [BoxGroup("Targeting & Usage")]
        [Tooltip("Which enemy/ally positions can this skill TARGET? (1-4)")]
        [ListDrawerSettings(ShowFoldout = false, AddCopiesLastElement = true)]
        public List<int> targetPositions = new List<int>() {1,2,3,4}; // Default to all

        [Title("Skill Ranks", titleAlignment: TitleAlignments.Centered)]
        [ListDrawerSettings(NumberOfItemsPerPage = 1, DraggableItems = true, ShowFoldout = true, ShowIndexLabels = true)]
        [Searchable]
        [ValidateInput("ValidateRanks", "All ranks must have a unique Rank Level, and levels must be sequential starting from 1.")]
        public List<SkillRankData> ranks = new List<SkillRankData> { new SkillRankData() }; // Start with one rank

        // Odin Inspector Validation
        private bool ValidateRanks(List<SkillRankData> rankList, ref string errorMessage)
        {
            if (rankList == null || rankList.Count == 0)
            {
                errorMessage = "Skill must have at least one rank.";
                return false;
            }
            HashSet<int> levels = new HashSet<int>();
            for (int i = 0; i < rankList.Count; i++)
            {
                if (rankList[i].rankLevel != i + 1)
                {
                    errorMessage = $"Rank levels must be sequential and start from 1. Error at index {i} (Rank Level {rankList[i].rankLevel}). Expected {i+1}.";
                    return false;
                }
                if (!levels.Add(rankList[i].rankLevel))
                {
                    errorMessage = $"Duplicate rank level found: {rankList[i].rankLevel}.";
                    return false;
                }
            }
            return true;
        }
        
        private bool IsSkillIdValid(string id, ref string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                errorMessage = "Skill ID cannot be empty. Try generating one.";
                return false;
            }
            // Optional: Add a check for uniqueness across all SkillDefinitionSO assets if needed,
            // but GUIDs are statistically very unlikely to collide.
            return true;
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                GenerateNewSkillId();
            }
        }

        // Example for ValueDropdown if you have a central animation name manager
        // private IEnumerable<string> GetAnimationTriggerNames() {
        //     // return AnimationNameManager.GetAllTriggers(); 
        //     return new List<string> { "Attack", "Skill1", "Skill2", "Heal" }; // Placeholder
        // }

        [Button("Add New Rank", ButtonSizes.Medium), PropertyOrder(10)]
        [GUIColor(0.6f, 1f, 0.6f)]
        private void AddNewRank()
        {
            int nextRankLevel = ranks.Count > 0 ? ranks.Max(r => r.rankLevel) + 1 : 1;
            ranks.Add(new SkillRankData { rankLevel = nextRankLevel });
        }
    }
}