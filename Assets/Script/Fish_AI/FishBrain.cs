// Scripts/Fish/Core/FishBrain.cs

using UnityEngine;
using System;

// ✏️ DITAMBAH: RequireComponent untuk auto-add components
[RequireComponent(typeof(FishMovement))]
[RequireComponent(typeof(FishDetectionSystem))]
public class FishBrain : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private AI_Fish_Data fishData;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;
    
    private FishMovement fishMovement;
    private FishDetectionSystem detectionSystem;
    
    private FishState currentState;
    private FishState previousState;
    
    private FishEnvironment environment = FishEnvironment.None;
    private float currentHealth;
    private bool isAlive = true;
    private Vector3 spawnPosition;
    
    // ✏️ DITAMBAH: Boundary reference dari zone
    private Bounds environmentBounds;
    private bool hasBounds = false;
    
    public AI_Fish_Data FishData => fishData;
    public FishEnvironment Environment => environment;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => isAlive;
    public Vector3 SpawnPosition => spawnPosition;
    public FishState CurrentState => currentState;
    public Bounds EnvironmentBounds => environmentBounds;
    public bool HasBounds => hasBounds;
    
    void Awake()
    {
        // ✏️ DIPERBAIKI: GetComponent dijamin sukses karena RequireComponent
        fishMovement = GetComponent<FishMovement>();
        detectionSystem = GetComponent<FishDetectionSystem>();
    }
    
    void Start()
    {
        Initialize();
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        if (currentState != null)
        {
            currentState.OnUpdate();
            
            Type nextStateType = currentState.CheckTransitions();
            if (nextStateType != null)
            {
                FishState nextState = (FishState)Activator.CreateInstance(nextStateType);
                TransitionToState(nextState);
            }
        }
    }
    
    public void Initialize()
    {
        if (fishData == null)
        {
            Debug.LogError("[FishBrain] FishData belum di-assign di Inspector!", this);
            return;
        }
        
        currentHealth = fishData.baseHealth;
        spawnPosition = transform.position;
        
        fishMovement.SetRotationSpeed(fishData.turnSpeed);
        fishMovement.SetAcceleration(fishData.acceleration);
        
        if (detectionSystem != null)
        {
            detectionSystem.SetDetectionRadius(fishData.detectionRange);
        }
        
        FishState initialState = DetermineInitialState();
        TransitionToState(initialState);
        
        Log($"Fish initialized: {fishData.ItemName} at {spawnPosition}");
    }
    
    private FishState DetermineInitialState()
    {
        switch (environment)
        {
            case FishEnvironment.Ocean:
                return new OceanPatrolState();
            
            case FishEnvironment.Aquarium:
                return new AquariumIdleState();
            
            default:
                return new OceanPatrolState();
        }
    }
    
    public void TransitionToState(FishState newState)
    {
        if (currentState != null)
        {
            Log($"State transition: {currentState.StateName} → {newState.StateName}");
            currentState.OnExit();
            previousState = currentState;
        }
        
        currentState = newState;
        currentState.Initialize(this);
        currentState.OnEnter();
    }
    
    public void ReturnToPreviousState()
    {
        if (previousState != null)
        {
            Log($"Returning to previous state: {previousState.StateName}");
            TransitionToState(previousState);
        }
        else
        {
            Log("No previous state, determining new state");
            TransitionToState(DetermineInitialState());
        }
    }
    
    // ✏️ DIPERBAIKI: Pass bounds ke FishMovement saat environment change
    public void OnEnvironmentChanged(FishEnvironment newEnvironment, Bounds bounds)
    {
        if (environment == newEnvironment) return;
        
        Log($"Environment changed: {environment} → {newEnvironment}");
        environment = newEnvironment;
        environmentBounds = bounds;
        hasBounds = true;
        
        // ✏️ DITAMBAH: Set boundary di FishMovement
        fishMovement.SetBoundary(bounds);
        
        FishState newState = DetermineInitialState();
        TransitionToState(newState);
    }
    
    public void OnCaught()
    {
        Log("Fish caught!");
        environment = FishEnvironment.Captured;
        
        if (currentState != null)
        {
            currentState.OnExit();
            currentState = null;
        }
        
        fishMovement.Stop();
        fishMovement.ClearBoundary();
    }
    
    public void OnPlacedInAquarium(AquariumZone aquarium)
    {
        Log("Fish placed in aquarium");
        Bounds aquariumBounds = aquarium.GetBounds();
        OnEnvironmentChanged(FishEnvironment.Aquarium, aquariumBounds);
    }
    
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0 && isAlive)
        {
            Die();
        }
        
        Log($"Took damage: {amount}. Health: {currentHealth}/{fishData.baseHealth}");
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(fishData.baseHealth, currentHealth);
        
        Log($"Healed: {amount}. Health: {currentHealth}/{fishData.baseHealth}");
    }
    
    private void Die()
    {
        isAlive = false;
        
        if (currentState != null)
        {
            currentState.OnExit();
            currentState = null;
        }
        
        fishMovement.Stop();
        
        Log("Fish died");
    }
    
    public string GetCurrentStateName()
    {
        return currentState != null ? currentState.StateName : "None";
    }
    
    public FishDetectionSystem GetDetectionSystem()
    {
        return detectionSystem;
    }
    
    public FishMovement GetMovement()
    {
        return fishMovement;
    }
    
    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log($"[FishBrain:{fishData.ItemName}] {message}", this);
        }
    }
}