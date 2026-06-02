// Scripts/Fish/FishFlockingBehavior.cs

using UnityEngine;
using System.Collections.Generic;

public class FishFlockingBehavior : MonoBehaviour
{
    [Header("Flocking Settings")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask fishLayer;
    
    [Header("Weights")]
    [Range(0f, 2f)]
    [SerializeField] private float cohesionWeight = 1f;
    
    [Range(0f, 2f)]
    [SerializeField] private float separationWeight = 1.5f;
    
    [Range(0f, 2f)]
    [SerializeField] private float alignmentWeight = 1f;
    
    [Header("Separation")]
    [SerializeField] private float minSeparationDistance = 1.5f;
    
    private FishMovement movement;
    private List<FishMovement> nearbyFish = new List<FishMovement>();
    
    void Awake()
    {
        movement = GetComponent<FishMovement>();
    }
    
    public Vector3 CalculateFlockingForce()
    {
        FindNearbyFish();
        
        if (nearbyFish.Count == 0)
            return Vector3.zero;
        
        Vector3 cohesion = CalculateCohesion();
        Vector3 separation = CalculateSeparation();
        Vector3 alignment = CalculateAlignment();
        
        Vector3 flockingForce = 
            (cohesion * cohesionWeight) +
            (separation * separationWeight) +
            (alignment * alignmentWeight);
        
        return flockingForce.normalized;
    }
    
    private void FindNearbyFish()
    {
        nearbyFish.Clear();
        
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            fishLayer
        );
        
        foreach (Collider col in colliders)
        {
            if (col.transform == transform) continue;
            
            FishMovement otherFish = col.GetComponent<FishMovement>();
            if (otherFish != null)
            {
                nearbyFish.Add(otherFish);
            }
        }
    }
    
    // COHESION: Gerak ke center kelompok
    private Vector3 CalculateCohesion()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;
        
        Vector3 centerOfMass = Vector3.zero;
        
        foreach (FishMovement fish in nearbyFish)
        {
            centerOfMass += fish.GetPosition();
        }
        
        centerOfMass /= nearbyFish.Count;
        
        return (centerOfMass - transform.position).normalized;
    }
    
    // SEPARATION: Hindari ikan terlalu dekat
    private Vector3 CalculateSeparation()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;
        
        Vector3 separationForce = Vector3.zero;
        
        foreach (FishMovement fish in nearbyFish)
        {
            float distance = Vector3.Distance(transform.position, fish.GetPosition());
            
            if (distance < minSeparationDistance && distance > 0)
            {
                Vector3 awayFromFish = transform.position - fish.GetPosition();
                separationForce += awayFromFish.normalized / distance;
            }
        }
        
        return separationForce.normalized;
    }
    
    // ALIGNMENT: Ikut arah mayoritas
    private Vector3 CalculateAlignment()
    {
        if (nearbyFish.Count == 0) return Vector3.zero;
        
        Vector3 averageDirection = Vector3.zero;
        
        foreach (FishMovement fish in nearbyFish)
        {
            averageDirection += fish.GetForward();
        }
        
        averageDirection /= nearbyFish.Count;
        
        return averageDirection.normalized;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minSeparationDistance);
    }
}
