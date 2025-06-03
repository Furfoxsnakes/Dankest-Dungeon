using UnityEngine;

public class CharacterVisuals : MonoBehaviour
{
    [SerializeField] private Character characterReference;
    
    private void Awake()
    {
        // Try to find the character reference if not explicitly set
        if (characterReference == null)
        {
            characterReference = GetComponentInParent<Character>();
            
            if (characterReference == null)
            {
                Debug.LogError($"[VISUALS] No Character component found for {gameObject.name}!");
            }
            else
            {
                Debug.Log($"[VISUALS] Found character reference: {characterReference.GetName()}");
            }
        }
    }
    
    // IMPORTANT: This is the method that should be called by animation events
    // Make sure your animation clips have events pointing to THIS method
    // Enhanced debug that shows WHEN the animation event fires
    public void AnimationComplete()
    {
        string animStateName = "Unknown";
        if (GetComponent<Animator>() != null)
        {
            animStateName = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Attack") ? "Attack" : 
                           GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Hit") ? "Hit" : 
                           GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle") ? "Idle" : "Other";
        }
        
        Debug.Log($"<color=magenta>[VISUALS] ⚠️ ANIMATION EVENT TRIGGERED for {gameObject.name}! Current state: {animStateName}</color>");
        
        if (characterReference != null)
        {
            Debug.Log($"<color=magenta>[VISUALS] Notifying character {characterReference.GetName()} of animation completion</color>");
            characterReference.OnAnimationComplete();
        }
        else
        {
            Debug.LogError($"<color=red>[VISUALS] Animation event triggered but no Character reference found!</color>");
        }
    }
}