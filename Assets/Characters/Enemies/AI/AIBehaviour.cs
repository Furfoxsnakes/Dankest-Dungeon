using UnityEngine;
using System.Collections.Generic;

public abstract class AIBehaviorSO : ScriptableObject
{
    public abstract BattleAction DecideAction(Character self, List<Character> allies, List<Character> enemies);
}