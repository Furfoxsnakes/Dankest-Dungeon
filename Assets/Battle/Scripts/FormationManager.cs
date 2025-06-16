using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Required for Linq operations like Where and Distinct if used

public class FormationManager : MonoBehaviour
{
    [Header("Formation Settings")]
    [SerializeField] private Transform heroFormationParent;
    [SerializeField] private Transform enemyFormationParent;
    
    [Header("Position Settings")]
    [SerializeField] private float spaceBetweenCharacters = 2.0f;
    [SerializeField] private Vector3 heroStartPosition = new Vector3(-8f, 0f, 0f); // Far left anchor for player formation
    [SerializeField] private Vector3 enemyStartPosition = new Vector3(4f, 0f, 0f); // Far left anchor for enemy formation
    
    private const int MAX_POSITIONS_PER_TEAM = 4; // Assuming 4 ranks (0, 1, 2, 3)

    private List<Character> spawnedPlayerCharacters = new List<Character>();
    private List<Character> spawnedEnemyCharacters = new List<Character>();
    
    public List<Character> SpawnFormation(FormationData formation)
    {
        if (formation == null)
        {
            Debug.LogError("Attempted to spawn null formation!");
            return new List<Character>();
        }
        
        bool isPlayerFormation = formation.formationType == FormationType.PlayerParty;
        
        if (isPlayerFormation)
        {
            ClearFormation(spawnedPlayerCharacters);
            spawnedPlayerCharacters.Clear();
        }
        else
        {
            ClearFormation(spawnedEnemyCharacters);
            spawnedEnemyCharacters.Clear();
        }
        
        List<Character> spawnedCharacters = new List<Character>();
        Transform parentTransform = isPlayerFormation ? heroFormationParent : enemyFormationParent;
        Vector3 baseStartPosition = isPlayerFormation ? heroStartPosition : enemyStartPosition;
        
        Debug.Log($"Spawning {(isPlayerFormation ? "Player" : "Enemy")} formation: {formation.name}");
        Debug.Log($"Base start position: {baseStartPosition}, Spacing: {spaceBetweenCharacters}");
        
        HashSet<int> usedLogicalRanks = new HashSet<int>();
        
        bool needsAutoPositioning = true;
        if (formation.slots.Any(s => s.characterPrefab != null && s.position >= 0 && s.position < MAX_POSITIONS_PER_TEAM))
        {
            bool allDefaultOrInvalidPositions = true;
            foreach(var s in formation.slots) {
                if (s.characterPrefab != null && s.position >= 0 && s.position < MAX_POSITIONS_PER_TEAM) { 
                    // A valid, explicitly set position exists
                    allDefaultOrInvalidPositions = false;
                    break;
                }
            }
            needsAutoPositioning = allDefaultOrInvalidPositions;
        }
        
        if (needsAutoPositioning && formation.slots.Any(s => s.characterPrefab != null))
        {
            Debug.Log("Auto-assigning logical ranks (0-indexed) for characters in formation.");
            int positionCounter = 0;
            foreach (var slot in formation.slots)
            {
                if (slot.characterPrefab != null)
                {
                    if (positionCounter < MAX_POSITIONS_PER_TEAM)
                    {
                        slot.position = positionCounter++; // Assigns 0, 1, 2, 3... as logical rank
                    }
                    else
                    {
                        Debug.LogWarning($"Too many characters for auto-positioning in formation {formation.name}. Max is {MAX_POSITIONS_PER_TEAM}. Character {slot.characterPrefab.name} will not be placed by auto-positioner.");
                        slot.position = -1; // Mark as unpositioned
                    }
                }
            }
        }
        
        foreach (var slot in formation.slots)
        {
            if (slot.characterPrefab == null || slot.position < 0 || slot.position >= MAX_POSITIONS_PER_TEAM) continue;
            
            int logicalRank = slot.position; // This is the 0-3 rank (0=front/melee, 3=back/ranged)

            if (usedLogicalRanks.Contains(logicalRank))
            {
                Debug.LogWarning($"Logical rank {logicalRank} already used in formation {formation.name} for {slot.characterPrefab.name}. Finding new rank.");
                int newRank = 0;
                while (usedLogicalRanks.Contains(newRank) && newRank < MAX_POSITIONS_PER_TEAM)
                {
                    newRank++;
                }
                if (newRank < MAX_POSITIONS_PER_TEAM)
                {
                    logicalRank = newRank;
                    slot.position = newRank; // Update slot if we changed it
                }
                else
                {
                    Debug.LogError($"Could not find an empty logical rank for {slot.characterPrefab.name} in formation {formation.name}. Character not spawned.");
                    continue;
                }
            }
            
            usedLogicalRanks.Add(logicalRank);
            
            Vector3 characterWorldPosition;

            if (isPlayerFormation)
            {
                // Player: Logical Rank 0 is far right, Logical Rank 3 is far left.
                // Visual offset from heroStartPosition (left anchor): (MAX_POSITIONS_PER_TEAM - 1 - logicalRank)
                float xOffset = (MAX_POSITIONS_PER_TEAM - 1 - logicalRank) * spaceBetweenCharacters;
                characterWorldPosition = baseStartPosition + new Vector3(xOffset, 0f, 0f);
                // Character.FormationPosition will be logicalRank (0=rightmost, 3=leftmost for player)
            }
            else // Enemy Formation
            {
                // Enemy: Logical Rank 0 is far left, Logical Rank 3 is far right.
                // Visual offset from enemyStartPosition (left anchor): logicalRank
                float xOffset = logicalRank * spaceBetweenCharacters;
                characterWorldPosition = baseStartPosition + new Vector3(xOffset, 0f, 0f);
                // Character.FormationPosition will be logicalRank (0=leftmost, 3=rightmost for enemy)
            }
            
            Debug.Log($"Spawning character {slot.characterPrefab.name} (Logical Rank: {logicalRank}) at world pos {characterWorldPosition} for {(isPlayerFormation ? "Player" : "Enemy")} team.");
            
            Character character = Instantiate(slot.characterPrefab, characterWorldPosition, Quaternion.identity, parentTransform);
            
            // Set FormationPosition (the logical rank 0-3 used for targeting)
            character.FormationPosition = logicalRank; 
            
            spawnedCharacters.Add(character);
            if (isPlayerFormation)
                spawnedPlayerCharacters.Add(character);
            else
                spawnedEnemyCharacters.Add(character);
        }
        
        return spawnedCharacters;
    }
    
    private void ClearFormation<T>(List<T> formation) where T : MonoBehaviour
    {
        foreach (var character in formation)
        {
            if (character != null)
                Destroy(character.gameObject);
        }
    }

    private RowCategory GetRowCategory(int formationPosition)
    {
        // formationPosition is the logical rank (0-3)
        // 0,1 are considered "Front" (melee)
        // 2,3 are considered "Back" (ranged)
        if (formationPosition == 0 || formationPosition == 1)
        {
            return RowCategory.Front;
        }
        if (formationPosition == 2 || formationPosition == 3)
        {
            return RowCategory.Back;
        }
        Debug.LogWarning($"Character is in an undefined row category with FormationPosition (Logical Rank): {formationPosition}");
        return RowCategory.Unknown;
    }

    public List<Character> GetCharactersInSameRowAs(Character referenceCharacter, List<Character> characterPool)
    {
        List<Character> charactersInRow = new List<Character>();

        if (referenceCharacter == null || !referenceCharacter.IsAlive)
        {
            Debug.LogWarning("GetCharactersInSameRowAs called with a null or dead reference character.");
            return charactersInRow;
        }

        // referenceCharacter.FormationPosition is the logical rank (0-3)
        RowCategory referenceRow = GetRowCategory(referenceCharacter.FormationPosition);

        if (referenceRow == RowCategory.Unknown)
        {
            Debug.LogWarning($"Reference character {referenceCharacter.GetName()} is in an Unknown row (Logical Rank: {referenceCharacter.FormationPosition}). Cannot determine row members.");
            return charactersInRow;
        }

        foreach (Character charInPool in characterPool)
        {
            if (charInPool != null && charInPool.IsAlive) 
            {
                RowCategory poolCharRow = GetRowCategory(charInPool.FormationPosition);
                if (poolCharRow == referenceRow)
                {
                    charactersInRow.Add(charInPool);
                }
            }
        }
        return charactersInRow;
    }

    public int GetCharacterRank(Character character)
    {
        if (character != null)
        {
            // This returns the logical rank (0-3)
            return character.FormationPosition;
        }
        Debug.LogWarning("Attempted to get rank of a null character. Returning -1.");
        return -1;
    }
}