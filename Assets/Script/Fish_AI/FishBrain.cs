// Scripts/Fish/FishBrain.cs
using System.Collections;
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
    
    private FishMovement movement;
    private FishFlockingBehavior flocking;
    private FishWanderBehavior wander;
    
    void Awake()
    {
        movement = GetComponent<FishMovement>();
        flocking = GetComponent<FishFlockingBehavior>();
        wander = GetComponent<FishWanderBehavior>();
    }
    
    void Update()
    {
        // Combine behaviors
        Vector3 flockingForce = flocking.CalculateFlockingForce();
        Vector3 wanderForce = wander.CalculateWanderForce();
        
        Vector3 finalDirection = 
            (flockingForce * flockingWeight) +
            (wanderForce * wanderWeight);
        
        // Execute movement
        if (finalDirection != Vector3.zero)
        {
            movement.Move(finalDirection);
        }
    }
    
    public void SetBoundary(Bounds bounds)
    {
        movement.SetBoundary(bounds);
    }
    
    public void OnCaptured()
    {
        // Notify spawner ikan ditangkap
        FishSpawner spawner = Object.FindFirstObjectByType<FishSpawner>();
        if (spawner != null)
        {
            spawner.OnFishCaptured();
        }
        
        Destroy(gameObject);
    }
}