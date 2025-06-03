using System.Collections.Generic;
using UnityEngine;

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
            if (slot.position > 0)
            {
                needsAutoPositioning = false;
                break;
            }
        }
        
        // Auto-assign positions if needed
        if (needsAutoPositioning)
        {
            Debug.Log("Auto-positioning characters in formation");
            int positionCounter = 0;
            foreach (var slot in formation.slots)
            {
                if (slot.characterPrefab != null)
                {
                    slot.position = positionCounter++;
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
            Debug.Log($"Spawning character at position {position}, slot position: {slot.position}");
            
            // Instantiate the appropriate character type
            Character character = Instantiate(slot.characterPrefab, position, Quaternion.identity, parentTransform);
            
            // Set position data for battle mechanics
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
}