using UnityEngine;

public class CharacterVisuals : MonoBehaviour
{
    [SerializeField] private Character characterReference;
    private Animator _animator; // Cache the animator

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            Debug.LogError($"[VISUALS] No Animator component found on {gameObject.name}!");
        }

        if (characterReference == null)
        {
            characterReference = GetComponentInParent<Character>();
            
            if (characterReference == null)
            {
                Debug.LogError($"[VISUALS] No Character component found for {gameObject.name} in parent hierarchy!");
            }
            else
            {
                Debug.Log($"[VISUALS] Found character reference: {characterReference.GetName()} for {gameObject.name}");
            }
        }
    }
    
    public void AnimationComplete() // Called by animation events
    {
        string animStateName = "Unknown";
        if (_animator != null) // Use cached animator
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            // A more robust way to get the state name if you have many states:
            // You might need to iterate through animator parameters or use a more direct way if IsName checks become too long.
            // For now, this is fine for a few common states.
            if (stateInfo.IsName("Attack")) animStateName = "Attack";
            else if (stateInfo.IsName("Hit")) animStateName = "Hit";
            else if (stateInfo.IsName("Idle")) animStateName = "Idle";
            else if (stateInfo.IsName("Defend")) animStateName = "Defend";
            else if (stateInfo.IsName("Cast")) animStateName = "Cast";
            else if (stateInfo.IsName("Death")) animStateName = "Death";
            // Add other state names as needed
            else animStateName = "Other/NotListed";
        }
        
        Debug.Log($"<color=magenta>[VISUALS] ⚠️ ANIMATION EVENT TRIGGERED for {gameObject.name}! Animator state: {animStateName}</color>");
        
        if (characterReference != null)
        {
            Debug.Log($"<color=magenta>[VISUALS] Notifying character {characterReference.GetName()} of animation completion.</color>");
            characterReference.OnAnimationComplete(); // This should now compile
        }
        else
        {
            Debug.LogError($"<color=red>[VISUALS] Animation event triggered on {gameObject.name} but no Character reference found!</color>");
        }
    }
}