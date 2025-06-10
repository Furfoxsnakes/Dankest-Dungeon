using UnityEngine;

public class CharacterVisuals : MonoBehaviour
{
    [SerializeField] private Character characterReference;
    private Animator _animator;

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
        }
    }

    public void PlayAnimation(AnimationTriggerName trigger)
    {
        if (_animator == null)
        {
            Debug.LogError($"[VISUALS] Animator is null on {gameObject.name}. Cannot play animation: {trigger}");
            return;
        }

        if (trigger == AnimationTriggerName.None)
        {
            // Optionally, you might want to set a default state like Idle or do nothing.
            // For now, we'll just avoid setting an empty trigger.
            // Debug.LogWarning($"[VISUALS] PlayAnimation called with AnimationTriggerName.None on {gameObject.name}. No trigger set.");
            return;
        }

        string triggerNameString = trigger.ToString(); // Get the string representation of the enum member

        if (!string.IsNullOrEmpty(triggerNameString))
        {
            Debug.Log($"[VISUALS] Playing animation on {characterReference?.GetName() ?? gameObject.name}: Triggering '{triggerNameString}'");
            _animator.SetTrigger(triggerNameString);
        }
        // This else case should ideally not be hit if AnimationTriggerName.None is handled
    }

    public void AnimationComplete() // Called by animation events
    {
        string animStateName = "Unknown";
        if (_animator != null) // Use cached animator
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            // This part remains tricky if state names don't exactly match trigger enum names.
            // Consider having animation events pass the AnimationTriggerName that started them,
            // or use Animator.StringToHash for more robust state checking if needed.
            if (stateInfo.IsName(AnimationTriggerName.Attack.ToString())) animStateName = AnimationTriggerName.Attack.ToString();
            else if (stateInfo.IsName(AnimationTriggerName.Hit.ToString())) animStateName = AnimationTriggerName.Hit.ToString();
            else if (stateInfo.IsName(AnimationTriggerName.Idle.ToString())) animStateName = AnimationTriggerName.Idle.ToString();
            else if (stateInfo.IsName(AnimationTriggerName.Defend.ToString())) animStateName = AnimationTriggerName.Defend.ToString();
            else if (stateInfo.IsName(AnimationTriggerName.Cast.ToString())) animStateName = AnimationTriggerName.Cast.ToString();
            else if (stateInfo.IsName(AnimationTriggerName.Death.ToString())) animStateName = AnimationTriggerName.Death.ToString();
            else animStateName = "Other/NotListed";
        }
        
        Debug.Log($"<color=magenta>[VISUALS] ⚠️ ANIMATION EVENT TRIGGERED for {gameObject.name}! Animator state: {animStateName}</color>");
        
        if (characterReference != null)
        {
            Debug.Log($"<color=magenta>[VISUALS] Notifying character {characterReference.GetName()} of animation completion.</color>");
            characterReference.OnAnimationComplete();
        }
        else
        {
            Debug.LogError($"<color=red>[VISUALS] Animation event triggered on {gameObject.name} but no Character reference found!</color>");
        }
    }
}