// Scripts/Fish/States/FishState.cs

using UnityEngine;

public abstract class FishState
{
    protected FishBrain brain;
    protected FishMovement movement;
    protected AI_Fish_Data data;
    protected Transform fishTransform;
    
    public virtual string StateName => GetType().Name;
    
    public virtual void Initialize(FishBrain ownerBrain)
    {
        brain = ownerBrain;
        movement = brain.GetComponent<FishMovement>();
        data = brain.FishData;
        fishTransform = brain.transform;
    }
    
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
    
    public virtual System.Type CheckTransitions()
    {
        return null;
    }
    
    public virtual void OnPlayerDetected(Transform player)
    {
        // Override di state yang perlu respond to player
    }
    
    public virtual void OnFoodDetected(Transform food)
    {
        // Override di state yang perlu respond to food
    }
    
    public virtual void OnBoundaryReached()
    {
        // Override di state yang perlu handle boundary
    }
}