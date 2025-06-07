using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Required for Linq operations like Where and Distinct if used

public class FormationManager : MonoBehaviour
{
    [Header("Formation Settings")]
    [SerializeField] private Transform heroFormationParent;
    [SerializeField] private Transform enemyFormationParent;
    
    [Header("Position Settings")]
    [SerializeField] private float spaceBetweenCharacters = 2.0f; // Increased spacing
    [SerializeField] private Vector3 heroStartPosition = new Vector3(-8f, 0f, 0f); // Far left
    [SerializeField] private Vector3 enemyStartPosition = new Vector3(4f, 0f, 0f); // Far right
    
    private List<Character> spawnedPlayerCharacters = new List<Character>();
    private List<Character> spawnedEnemyCharacters = new List<Character>();
    
    // Updated method that takes direct FormationData reference
    public List<Character> SpawnFormation(FormationData formation)
    {
        if (formation == null)
        {
            Debug.LogError("Attempted to spawn null formation!");
            return new List<Character>();
        }
        
        bool isPlayerFormation = formation.formationType == FormationType.PlayerParty;
        
        // Clear existing characters
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
        Vector3 startPosition = isPlayerFormation ? heroStartPosition : enemyStartPosition;
        
        // Debug formation details
        Debug.Log($"Spawning {(isPlayerFormation ? "Player" : "Enemy")} formation: {formation.name}");
        Debug.Log($"Start position: {startPosition}, Spacing: {spaceBetweenCharacters}");
        
        // Track used positions to avoid overlaps
        HashSet<int> usedPositions = new HashSet<int>();
        
        // If no positions are set in formation, assign sequential positions
        bool needsAutoPositioning = true;
        foreach (var slot in formation.slots)
        {
            if (slot.characterPrefab != null && slot.position >= 0) // Check if position is explicitly set
            {
                // A more robust check would be if any slot has a position > 0,
                // or if all character-holding slots have position 0.
                // For simplicity, if any slot has a non-default position, assume manual setup.
                // This part of the logic might need refinement based on how FormationData is authored.
                // Let's assume if any slot.position is > 0, it's not pure auto-positioning.
                // A simple check: if all slot.positions for non-null prefabs are 0, then auto-position.
                bool allDefaultPositions = true;
                foreach(var s in formation.slots) {
                    if (s.characterPrefab != null && s.position != 0) { // Assuming 0 is default/unset for auto
                        allDefaultPositions = false;
                        break;
                    }
                }
                needsAutoPositioning = allDefaultPositions;
                if (!needsAutoPositioning) break; // Found a non-default position, stop checking
            }
        }
        
        // Auto-assign positions if needed
        if (needsAutoPositioning && formation.slots.Any(s => s.characterPrefab != null))
        {
            Debug.Log("Auto-positioning characters in formation (0-indexed).");
            int positionCounter = 0;
            foreach (var slot in formation.slots)
            {
                if (slot.characterPrefab != null)
                {
                    slot.position = positionCounter++; // Assigns 0, 1, 2, 3...
                }
            }
        }
        
        foreach (var slot in formation.slots)
        {
            if (slot.characterPrefab == null) continue;
            
            // Ensure each character has a unique position
            if (usedPositions.Contains(slot.position))
            {
                Debug.LogWarning($"Position {slot.position} already used in formation. Adjusting position.");
                int newPosition = 0;
                while (usedPositions.Contains(newPosition))
                {
                    newPosition++;
                }
                slot.position = newPosition;
            }
            
            usedPositions.Add(slot.position);
            
            // Position characters horizontally (left to right)
            Vector3 position = startPosition + new Vector3(slot.position * spaceBetweenCharacters, 0f, 0f);
            
            // Debug individual character position
            Debug.Log($"Spawning character {slot.characterPrefab.name} at world pos {position}, formation rank (0-indexed): {slot.position}");
            
            // Instantiate the appropriate character type
            Character character = Instantiate(slot.characterPrefab, position, Quaternion.identity, parentTransform);
            
            // Set position data for battle mechanics (0-indexed)
            character.FormationPosition = slot.position; 
            
            // Add to appropriate list
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

    /// <summary>
    /// Determines the row category (Front/Back) of a character based on their 0-indexed formation position.
    /// Assumes a 4-rank system: Positions 0,1 are Front; Positions 2,3 are Back.
    /// </summary>
    private RowCategory GetRowCategory(int formationPosition)
    {
        if (formationPosition == 0 || formationPosition == 1)
        {
            return RowCategory.Front;
        }
        if (formationPosition == 2 || formationPosition == 3)
        {
            return RowCategory.Back;
        }
        // Fallback for unexpected positions, or if you have more than 4 ranks.
        // This might indicate an issue with FormationPosition assignment or a different rank system.
        Debug.LogWarning($"Character is in an undefined row category with FormationPosition: {formationPosition}");
        return RowCategory.Unknown;
    }

    /// <summary>
    /// Gets all characters from the characterPool that are in the same row as the referenceCharacter.
    /// Assumes Character.FormationPosition is 0-indexed (0,1 = Front; 2,3 = Back).
    /// </summary>
    /// <param name="referenceCharacter">The character whose row is used as a reference.</param>
    /// <param name="characterPool">The list of characters to check against (e.g., all allies or all enemies).</param>
    /// <returns>A list of characters in the same row as the reference character.</returns>
    public List<Character> GetCharactersInSameRowAs(Character referenceCharacter, List<Character> characterPool)
    {
        List<Character> charactersInRow = new List<Character>();

        if (referenceCharacter == null || !referenceCharacter.IsAlive)
        {
            Debug.LogWarning("GetCharactersInSameRowAs called with a null or dead reference character.");
            return charactersInRow; // Return empty list
        }

        RowCategory referenceRow = GetRowCategory(referenceCharacter.FormationPosition);

        if (referenceRow == RowCategory.Unknown)
        {
            // This implies the referenceCharacter is not in a standard front/back position.
            // Depending on game rules, you might want to return just the referenceCharacter if it's in the pool,
            // or an empty list. For now, returning empty as it's an "unknown" row.
            Debug.LogWarning($"Reference character {referenceCharacter.GetName()} is in an Unknown row (Position: {referenceCharacter.FormationPosition}). Cannot determine row members.");
            return charactersInRow;
        }

        foreach (Character charInPool in characterPool)
        {
            // The input characterPool might already be filtered for IsAlive().
            // Adding a check here for robustness of this specific method.
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

    /// <summary>
    /// Gets the 0-indexed rank/position of a character.
    /// This is a simple accessor to Character.FormationPosition.
    /// </summary>
    public int GetCharacterRank(Character character)
    {
        if (character != null)
        {
            return character.FormationPosition;
        }
        Debug.LogWarning("Attempted to get rank of a null character. Returning -1.");
        return -1; // Invalid rank
    }
}