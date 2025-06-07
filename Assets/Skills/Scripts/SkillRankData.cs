using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using DankestDungeon.Skills; // Your enums namespace

namespace DankestDungeon.Skills
{
    [System.Serializable]
    public class SkillRankData
    {
        [ProgressBar(1, 5, r:0.2f, g:0.8f, b:0.2f, Height = 20), Range(1,5)] // Example max rank 5
        [GUIColor(0.7f, 1f, 0.7f)]
        public int rankLevel = 1;

        [Tooltip("Localization key for the rank-specific description.")]
        public string rankDescriptionKey;
        
        [TextArea(3,5)]
        [InfoBox("This is the in-editor preview/notes for the description. Use localization keys for actual game text.")]
        public string editorDescriptionPreview;

        [SuffixLabel("MP", Overlay = true)]
        public int manaCost;
        
        [SuffixLabel("Turns", Overlay = true)]
        [MinValue(0)]
        public int cooldown; // 0 for no cooldown

        [Tooltip("Accuracy modifier for this rank of the skill. Can be negative.")]
        [SuffixLabel("%", Overlay = true)]
        public int accuracyMod;

        [Tooltip("Critical hit chance modifier for this rank. Can be negative.")]
        [SuffixLabel("%", Overlay = true)]
        public float critMod;

        [ListDrawerSettings(NumberOfItemsPerPage = 1, ShowFoldout = true)]
        public List<SkillEffectData> effects = new List<SkillEffectData>();
    }
}