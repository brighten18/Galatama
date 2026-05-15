// Scripts/Fish/States/AquariumIdleState.cs

using UnityEngine;

public class AquariumIdleState : FishState
{
    private Vector3 swimDirection;
    private float timeToChangeDirection;
    private float changeDirectionInterval;
    private float wallAvoidDistance = 2.0f;
    
    public override void OnEnter()
    {
        changeDirectionInterval = data.changeDirectionInterval;
        timeToChangeDirection = changeDirectionInterval;
        
        PickRandomDirection();
        
        Debug.Log($"[AquariumIdle] Entered");
    }
    
    public override void OnUpdate()
    {
        // ✏️ DIPERBAIKI: Boundary check menggunakan FishMovement
        if (movement.IsNearBoundary(wallAvoidDistance))
        {
            Vector3 awayFromBoundary = movement.GetDirectionAwayFromBoundary();
            swimDirection = awayFromBoundary;
            Debug.Log($"[AquariumIdle] Near boundary, redirecting");
        }
        
        movement.MoveInDirection(swimDirection, data.aquariumIdleSpeed);
        
        timeToChangeDirection -= Time.deltaTime;
        if (timeToChangeDirection <= 0)
        {
            PickRandomDirection();
            timeToChangeDirection = changeDirectionInterval;
        }
    }
    
    public override void OnExit()
    {
        Debug.Log("[AquariumIdle] Exited");
    }
    
    public override System.Type CheckTransitions()
    {
        // Future: transition to FeedingState
        return null;
    }
    
    private void PickRandomDirection()
    {
        swimDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.3f, 0.3f),
            Random.Range(-1f, 1f)
        ).normalized;
        
        Debug.Log($"[AquariumIdle] New direction: {swimDirection}");
    }
}