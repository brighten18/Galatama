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

    void Awake()
    {
        movement = GetComponent<FishMovement>();
        flocking = GetComponent<FishFlockingBehavior>();
        wander = GetComponent<FishWanderBehavior>();
    }

    void Update()
    {
        if (isCaptured) return;

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

    public void OnCaptured(bool destroyObject = true)
    {
        if (isCaptured) return;
        isCaptured = true;

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
}