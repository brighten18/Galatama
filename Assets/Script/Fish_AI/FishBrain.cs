using UnityEngine;

[RequireComponent(typeof(FishMovement))]
[RequireComponent(typeof(FishFlockingBehavior))]
[RequireComponent(typeof(FishWanderBehavior))]
public class FishBrain : MonoBehaviour
{
    [Header("Behavior Weights")]
    [Range(0f, 1f)]
    [SerializeField] private float flockingWeight = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float wanderWeight = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float boundaryWeight = 0.8f;

    private FishMovement movement;
    private FishFlockingBehavior flocking;
    private FishWanderBehavior wander;
    private FishSpawner ownerSpawner;
    private bool isCaptured;
    private bool hasTemporaryTarget;
    private Vector3 temporaryTarget;
    private float temporaryTargetStopDistance = 0.5f;

    void Awake()
    {
        movement = GetComponent<FishMovement>();
        flocking = GetComponent<FishFlockingBehavior>();
        wander = GetComponent<FishWanderBehavior>();
    }

    void Update()
    {
        if (isCaptured) return;

        if (hasTemporaryTarget)
        {
            Vector3 targetDirection = temporaryTarget - transform.position;
            if (targetDirection.sqrMagnitude > temporaryTargetStopDistance * temporaryTargetStopDistance)
            {
                movement.Move(targetDirection, 1.25f);
            }

            return;
        }

        Vector3 flockingForce = flocking.CalculateFlockingForce();
        Vector3 wanderForce = wander.CalculateWanderForce();
        Vector3 boundaryForce = movement.GetBoundarySteering();

        Vector3 finalDirection =
            flockingForce * flockingWeight +
            wanderForce * wanderWeight +
            boundaryForce * boundaryWeight;

        if (finalDirection.sqrMagnitude > 0.0001f)
        {
            movement.Move(finalDirection);
        }
    }

    public void SetBoundary(Bounds bounds)
    {
        movement.SetBoundary(bounds);
    }

    public void SetSpawner(FishSpawner spawner)
    {
        ownerSpawner = spawner;
    }

    public void SetTemporaryTarget(Vector3 target, float stopDistance)
    {
        temporaryTarget = target;
        temporaryTargetStopDistance = Mathf.Max(0.05f, stopDistance);
        hasTemporaryTarget = true;
    }

    public void ClearTemporaryTarget()
    {
        hasTemporaryTarget = false;
    }

    public void OnCaptured(bool destroyObject = true)
    {
        if (isCaptured) return;
        isCaptured = true;
        hasTemporaryTarget = false;

        FishSpawner spawner = ownerSpawner != null ? ownerSpawner : Object.FindFirstObjectByType<FishSpawner>();
        if (spawner != null)
        {
            spawner.OnFishCaptured();
        }

        if (destroyObject)
        {
            Destroy(gameObject);
        }
    }

    public bool IsCaptured => isCaptured;
}
