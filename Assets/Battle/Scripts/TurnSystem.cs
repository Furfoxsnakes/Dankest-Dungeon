using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnSystem : MonoBehaviour
{
    private List<Character> allCharacters = new List<Character>();
    private Queue<Character> turnOrder = new Queue<Character>();
    private Character currentActor;

    public void Initialize(List<Character> playerCharacters, List<Character> enemyCharacters)
    {
        // Store all characters
        allCharacters.Clear();
        allCharacters.AddRange(playerCharacters);
        allCharacters.AddRange(enemyCharacters);
        
        // Reset turn queue
        ResetTurnOrder();
        
        // Set first actor
        if (turnOrder.Count > 0)
            currentActor = turnOrder.Dequeue();
    }
    
    public Character GetCurrentActor()
    {
        return currentActor;
    }
    
    public Character GetNextActor()
    {
        // If turn order is empty, reset it
        if (turnOrder.Count == 0)
            ResetTurnOrder();
            
        // Get next character in queue
        currentActor = turnOrder.Dequeue();
        
        // Skip dead characters
        while (!currentActor.IsAlive() && turnOrder.Count > 0)
        {
            currentActor = turnOrder.Dequeue();
        }
        
        // If all characters in queue are dead, reset turn order and try again
        if (!currentActor.IsAlive())
        {
            ResetTurnOrder();
            return GetNextActor();
        }
        
        return currentActor;
    }
    
    private void ResetTurnOrder()
    {
        // Clear the queue
        turnOrder.Clear();
        
        // Sort characters by speed (higher goes first)
        var sortedCharacters = allCharacters
            .Where(c => c.IsAlive())
            .OrderByDescending(c => c.Stats.speed)  // Direct access to speed stat
            .ToList();
            
        // Add all living characters to turn queue
        foreach (var character in sortedCharacters)
        {
            turnOrder.Enqueue(character);
        }
    }
}