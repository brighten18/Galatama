// Scripts/Fish/Core/FishData.cs

using UnityEngine;

[CreateAssetMenu(fileName = "AI_FishData", menuName = "Fish_AI/FishData-AI")]
public class AI_Fish_Data : ScriptableObject
{
    [Header("Species Info")]
    public string ItemName = "Tuna";
    
    [Header("Movement Parameters")]
    [Tooltip("Normal swim speed")]
    public float baseSwimSpeed = 3.0f;
    
    [Tooltip("Speed when fleeing from threat")]
    public float fleeSpeed = 6.0f;
    
    [Tooltip("Swim speed in aquarium (slower, calmer)")]
    public float aquariumIdleSpeed = 1.5f;
    
    [Tooltip("How fast the fish rotates")]
    public float turnSpeed = 120f;
    
    [Tooltip("Acceleration rate")]
    public float acceleration = 2.0f;
    
    [Header("Behavior Parameters")]
    [Tooltip("Radius for random wander offset")]
    public float wanderRadius = 2.0f;
    
    [Tooltip("Max distance from spawn point for patrol")]
    public float patrolRadius = 20.0f;
    
    [Tooltip("Safe distance from threat")]
    public float fleeDistance = 10.0f;
    
    [Tooltip("Detection range for player/food/threats")]
    public float detectionRange = 8.0f;
    
    [Tooltip("Time between direction changes in aquarium")]
    public float changeDirectionInterval = 3.0f;
    
    [Header("Physical Properties")]
    [Tooltip("Base health for this species")]
    public float baseHealth = 100f;
    
    [Tooltip("Weight in kg (for simulation/physics)")]
    public float weight = 5.0f;
}