// Scripts/Fish/Utility/FishAnimationController.cs

using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FishAnimationController : MonoBehaviour
{
    private Animator animator;
    private FishMovement movement;
    
    // Animator parameter names
    private readonly int speedParam = Animator.StringToHash("Speed");
    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int caughtTrigger = Animator.StringToHash("Caught");
    private readonly int feedTrigger = Animator.StringToHash("Feed");
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<FishMovement>();
    }
    
    void Update()
    {
        if (animator == null || movement == null) return;
        
        UpdateAnimator();
    }
    
    private void UpdateAnimator()
    {
        float currentSpeed = movement.GetCurrentSpeed();
        
        animator.SetFloat(speedParam, currentSpeed);
        animator.SetBool(isMovingParam, currentSpeed > 0.1f);
    }
    
    public void TriggerCaught()
    {
        if (animator != null)
        {
            animator.SetTrigger(caughtTrigger);
        }
    }
    
    public void TriggerFeed()
    {
        if (animator != null)
        {
            animator.SetTrigger(feedTrigger);
        }
    }
}