// Scripts/Fish/FishWanderBehavior.cs

using UnityEngine;

public class FishWanderBehavior : MonoBehaviour
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float changeTargetInterval = 3f;
    
    private Vector3 wanderTarget;
    private float changeTimer;
    
    void Start()
    {
        GenerateNewWanderTarget();
        changeTimer = changeTargetInterval;
    }
    
    void Update()
    {
        changeTimer -= Time.deltaTime;
        
        if (changeTimer <= 0f)
        {
            GenerateNewWanderTarget();
            changeTimer = changeTargetInterval;
        }
    }
    
    public Vector3 CalculateWanderForce()
    {
        return wanderTarget.normalized;
    }
    
    private void GenerateNewWanderTarget()
    {
        wanderTarget = new Vector3(
            Random.Range(-wanderRadius, wanderRadius),
            Random.Range(-wanderRadius * 0.3f, wanderRadius * 0.3f),
            Random.Range(-wanderRadius, wanderRadius)
        );
    }
}