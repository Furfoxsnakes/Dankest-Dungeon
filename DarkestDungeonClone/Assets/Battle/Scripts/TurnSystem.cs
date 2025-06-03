using System.Collections.Generic;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    private List<Character> charactersInTurnOrder;
    private int currentTurnIndex;

    void Start()
    {
        charactersInTurnOrder = new List<Character>();
        currentTurnIndex = 0;
    }

    public void AddCharacterToTurnOrder(Character character)
    {
        charactersInTurnOrder.Add(character);
    }

    public void NextTurn()
    {
        if (charactersInTurnOrder.Count == 0) return;

        currentTurnIndex = (currentTurnIndex + 1) % charactersInTurnOrder.Count;
        ExecuteTurn(charactersInTurnOrder[currentTurnIndex]);
    }

    private void ExecuteTurn(Character character)
    {
        // Logic for executing the character's turn
        // This could involve checking if the character can act, displaying UI, etc.
    }

    public Character GetCurrentCharacter()
    {
        if (charactersInTurnOrder.Count == 0) return null;
        return charactersInTurnOrder[currentTurnIndex];
    }
}