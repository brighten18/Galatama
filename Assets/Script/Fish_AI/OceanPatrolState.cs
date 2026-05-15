// Scripts/Fish/States/OceanPatrolState.cs

using UnityEngine;

public class OceanPatrolState : FishState
{
    private Vector3 currentWaypoint;
    private Vector3 currentWanderOffset;
    private Vector3 currentTarget;

    private Vector3 patrolCenter;
    private float patrolRadius;

    private float waypointReachThreshold = 2.0f;
    private float boundaryCheckDistance = 2.5f;
    private float wanderChangeInterval = 1.5f;
    private float wanderTimer;

    private Transform detectedPlayer;

    public override void OnEnter()
    {
        patrolCenter = brain.SpawnPosition;
        patrolRadius = data.patrolRadius;

        GenerateNewWaypoint();
        GenerateNewWanderOffset();

        Debug.Log($"[OceanPatrol] Entered. Center: {patrolCenter}, Radius: {patrolRadius}");
    }

    public override void OnUpdate()
    {
        detectedPlayer = brain.GetDetectionSystem().DetectPlayer();

        if (movement.HasBoundary() && movement.IsNearBoundary(boundaryCheckDistance))
        {
            Vector3 directionToCenter = movement.GetDirectionAwayFromBoundary();
            movement.MoveInDirection(directionToCenter, data.baseSwimSpeed);
            return;
        }

        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            GenerateNewWanderOffset();
        }

        currentTarget = currentWaypoint + currentWanderOffset;

        if (movement.HasBoundary())
        {
            currentTarget = movement.ClampPointInsideBoundary(currentTarget);
        }

        movement.MoveToPoint(currentTarget, data.baseSwimSpeed);

        float distanceToWaypoint = Vector3.Distance(fishTransform.position, currentWaypoint);

        if (distanceToWaypoint < waypointReachThreshold)
        {
            GenerateNewWaypoint();
            GenerateNewWanderOffset();
        }
    }

    public override void OnExit()
    {
        Debug.Log("[OceanPatrol] Exited");
    }

    public override System.Type CheckTransitions()
    {
        if (detectedPlayer != null)
        {
            float distanceToPlayer = Vector3.Distance(fishTransform.position, detectedPlayer.position);

            if (distanceToPlayer < data.fleeDistance)
            {
                return typeof(FleeState);
            }
        }

        return null;
    }

    private void GenerateNewWaypoint()
    {
        if (brain.HasBounds)
        {
            Bounds bounds = brain.EnvironmentBounds;

            currentWaypoint = new Vector3(
                Random.Range(bounds.min.x + 2f, bounds.max.x - 2f),
                Random.Range(bounds.min.y + 1f, bounds.max.y - 1f),
                Random.Range(bounds.min.z + 2f, bounds.max.z - 2f)
            );
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;

            currentWaypoint = patrolCenter + new Vector3(
                randomCircle.x,
                Random.Range(-2f, 2f),
                randomCircle.y
            );
        }

        Debug.Log($"[OceanPatrol] New waypoint: {currentWaypoint}");
    }

    private void GenerateNewWanderOffset()
    {
        currentWanderOffset = movement.GetRandomWanderOffset(data.wanderRadius);
        wanderTimer = wanderChangeInterval;
    }
}