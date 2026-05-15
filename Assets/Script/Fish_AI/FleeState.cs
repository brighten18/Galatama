// Scripts/Fish/States/FleeState.cs

using UnityEngine;

public class FleeState : FishState
{
    private Transform threatTransform;
    private Vector3 fleeDirection;
    private float safeDistance;
    private float fleeTimer;
    private float maxFleeTime = 5.0f;
    
    public override void OnEnter()
    {
        // Get threat from detection system
        threatTransform = brain.GetDetectionSystem().DetectPlayer();
        
        if (threatTransform == null)
        {
            Debug.LogWarning("[Flee] No threat detected, returning to previous state");
            brain.ReturnToPreviousState();
            return;
        }
        
        safeDistance = data.fleeDistance;
        fleeTimer = maxFleeTime;
        
        CalculateFleeDirection();
        
        Debug.Log($"[Flee] Entered. Fleeing from: {threatTransform.name}");
    }
    
    public override void OnUpdate()
    {
        if (threatTransform == null)
        {
            brain.ReturnToPreviousState();
            return;
        }
        
        // Recalculate flee direction (threat might be moving)
        CalculateFleeDirection();
        
        // Move away from threat at high speed
        movement.MoveInDirection(fleeDirection, data.fleeSpeed);
        
        // Decrement timer
        fleeTimer -= Time.deltaTime;
    }
    
    public override void OnExit()
    {
        Debug.Log("[Flee] Exited");
    }
    
    public override System.Type CheckTransitions()
    {
        // Return to previous state if safe distance reached or timeout
        if (IsSafeDistance() || fleeTimer <= 0)
        {
            Debug.Log("[Flee] Safe distance reached or timeout");
            brain.ReturnToPreviousState();
            return null;
        }
        
        return null;
    }
    
    private void CalculateFleeDirection()
    {
        Vector3 toThreat = threatTransform.position - fishTransform.position;
        fleeDirection = -toThreat.normalized;
    }
    
    private bool IsSafeDistance()
    {
        if (threatTransform == null) return true;
        
        float distance = Vector3.Distance(fishTransform.position, threatTransform.position);
        return distance >= safeDistance;
    }
}