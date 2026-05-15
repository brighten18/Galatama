using UnityEngine;

public class FishMainMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float acceleration = 2.0f;
    [SerializeField] private float smoothing = 0.1f;
    
    private Transform fishTransform;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private Vector3 currentVelocity = Vector3.zero;
    
    void Awake()
    {
        fishTransform = transform;
    }
    
    void Update()
    {
        // Smooth speed transition
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);
    }
    
    public void MoveToPoint(Vector3 targetPosition, float speed)
    {
        targetSpeed = speed;
        
        Vector3 direction = (targetPosition - fishTransform.position).normalized;
        
        if (direction != Vector3.zero)
        {
            RotateTowards(direction);
            
            Vector3 movement = direction * currentSpeed * Time.deltaTime;
            fishTransform.position += movement;
        }
    }
    
    public void MoveInDirection(Vector3 direction, float speed)
    {
        targetSpeed = speed;
        
        if (direction != Vector3.zero)
        {
            RotateTowards(direction);
            
            Vector3 movement = direction.normalized * currentSpeed * Time.deltaTime;
            fishTransform.position += movement;
        }
    }
    
    public void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        fishTransform.rotation = Quaternion.RotateTowards(
            fishTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    
    public Vector3 ApplyRandomWander(float radius)
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-radius, radius),
            Random.Range(-radius * 0.3f, radius * 0.3f),
            Random.Range(-radius, radius)
        );
        
        return randomOffset;
    }
    
    public void Stop()
    {
        targetSpeed = 0f;
    }
    
    public void SetSpeed(float speed)
    {
        targetSpeed = speed;
    }
    
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
    
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = speed;
    }
    
    public void SetAcceleration(float accel)
    {
        acceleration = accel;
    }
}
