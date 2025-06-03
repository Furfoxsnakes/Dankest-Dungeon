using System.Collections.Generic;
using UnityEngine;

public enum FormationType
{
    PlayerParty,
    EnemyGroup
}

[CreateAssetMenu(fileName = "NewFormation", menuName = "Dankest Dungeon/Formation")]
public class FormationData : ScriptableObject
{
    public string formationName;
    public FormationType formationType;
    public List<FormationSlot> slots = new List<FormationSlot>();
    
    [System.Serializable]
    public class FormationSlot
    {
        public int position; // 0-3 front to back
        public Character characterPrefab;
        
        // Optional: Add weight for random formation generation
        [Range(0, 100)]
        public int weight = 100;
    }
}