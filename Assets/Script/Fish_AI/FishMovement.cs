// Scripts/Fish/FishMovement.cs - FIXED VERSION

using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 120f;
    
    [Header("Model Orientation")]
    [SerializeField] private ForwardDirection modelForward = ForwardDirection.Z_Positive;
    
    [Header("Boundary")]
    [SerializeField] private float boundaryPadding = 1f;
    [SerializeField] private bool lockYPosition = false; // ✏️ DITAMBAH
    [SerializeField] private float fixedYPosition = 5f;   // ✏️ DITAMBAH
    
    private Transform fishTransform;
    private Bounds zoneBounds;
    private bool hasBounds = false;
    
    void Awake()
    {
        fishTransform = transform;
    }
    
    public void Move(Vector3 direction, float speedMultiplier = 1f)
    {
        if (direction == Vector3.zero) return;
        
        // ✏️ FIX: Normalize direction untuk prevent scaling issues
        direction = direction.normalized;
        
        // ✏️ FIX: Lock Y jika enabled (untuk prevent turun/naik)
        if (lockYPosition)
        {
            direction.y = 0f;
            direction = direction.normalized;
        }
        
        // Rotate - FIX: Hanya rotate di Y axis (heading)
        RotateTowards(direction);
        
        // Move
        Vector3 newPosition = fishTransform.position + (direction * moveSpeed * speedMultiplier * Time.deltaTime);
        
        // ✏️ FIX: Lock Y position jika enabled
        if (lockYPosition)
        {
            newPosition.y = fixedYPosition;
        }
        
        fishTransform.position = newPosition;
        
        // Enforce boundary
        if (hasBounds)
        {
            EnforceBoundary();
        }
    }
    
    // ✏️ FIX: Simplified rotation - hanya Y axis (heading)
    private void RotateTowards(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        
        // ✏️ FIX: Force Y to 0 untuk horizontal-only rotation
        direction.y = 0f;
        if (direction == Vector3.zero) return;
        
        // Get target rotation berdasarkan model forward
        Quaternion targetRotation = GetRotationForDirection(direction);
        
        // ✏️ FIX: Lock rotation hanya di Y axis (heading)
        Vector3 currentEuler = fishTransform.rotation.eulerAngles;
        Vector3 targetEuler = targetRotation.eulerAngles;
        
        // Interpolate hanya Y rotation
        float newYRotation = Mathf.LerpAngle(currentEuler.y, targetEuler.y, rotationSpeed * Time.deltaTime / 120f);
        
        // ✏️ FIX: Set rotation dengan X=0, Z=0 (no roll/pitch)
        fishTransform.rotation = Quaternion.Euler(0f, newYRotation, 0f);
    }
    
    private Quaternion GetRotationForDirection(Vector3 worldDirection)
    {
        // Normalize dan force Y = 0
        worldDirection.y = 0f;
        if (worldDirection == Vector3.zero) return fishTransform.rotation;
        
        Vector3 modelForwardVector = GetModelForwardVector();
        
        // Calculate base rotation
        Quaternion lookRotation = Quaternion.LookRotation(worldDirection);
        
        // Apply model forward offset
        Quaternion offsetRotation = Quaternion.FromToRotation(Vector3.forward, modelForwardVector);
        
        return lookRotation * Quaternion.Inverse(offsetRotation);
    }
    
    private Vector3 GetModelForwardVector()
    {
        switch (modelForward)
        {
            case ForwardDirection.Z_Positive: return Vector3.forward;
            case ForwardDirection.Z_Negative: return Vector3.back;
            case ForwardDirection.X_Positive: return Vector3.right;
            case ForwardDirection.X_Negative: return Vector3.left;
            case ForwardDirection.Y_Positive: return Vector3.up;
            case ForwardDirection.Y_Negative: return Vector3.down;
            default: return Vector3.forward;
        }
    }
    
    public void SetBoundary(Bounds bounds)
    {
        zoneBounds = bounds;
        hasBounds = true;
        
        // ✏️ DITAMBAH: Set fixed Y position dari center bounds
        if (lockYPosition)
        {
            fixedYPosition = bounds.center.y;
        }
    }
    
    // ✏️ FIX: Simplified boundary enforcement
   // Di FishMovement.cs - UPDATE METHOD INI

    private void EnforceBoundary()
    {
        if (!hasBounds) return; // ✏️ DITAMBAH: Safety check
        
        Vector3 pos = fishTransform.position;
        Vector3 min = zoneBounds.min + Vector3.one * boundaryPadding;
        Vector3 max = zoneBounds.max - Vector3.one * boundaryPadding;
        
        bool wasOutside = false;
        
        // ✏️ DIPERBAIKI: Separate checking untuk setiap axis
        if (pos.x < min.x)
        {
            pos.x = min.x;
            wasOutside = true;
        }
        else if (pos.x > max.x)
        {
            pos.x = max.x;
            wasOutside = true;
        }
        
        if (pos.z < min.z)
        {
            pos.z = min.z;
            wasOutside = true;
        }
        else if (pos.z > max.z)
        {
            pos.z = max.z;
            wasOutside = true;
        }
        
        // Y clamping (jika tidak lock Y)
        if (!lockYPosition)
        {
            if (pos.y < min.y)
            {
                pos.y = min.y;
                wasOutside = true;
            }
            else if (pos.y > max.y)
            {
                pos.y = max.y;
                wasOutside = true;
            }
        }
        else
        {
            pos.y = fixedYPosition;
        }
        
        if (wasOutside)
        {
            fishTransform.position = pos;
            
            // ✏️ DITAMBAH: Log warning saat enforce
            Debug.LogWarning($"[FishMovement] {gameObject.name} was outside bounds at {fishTransform.position}, clamped to {pos}");
            
            // Redirect ke center (horizontal only)
            Vector3 toCenter = zoneBounds.center - pos;
            toCenter.y = 0f;
            
            if (toCenter != Vector3.zero)
            {
                // ✏️ DIPERBAIKI: Langsung set rotation (tidak smooth) untuk instant redirect
                Vector3 targetDir = toCenter.normalized;
                Quaternion targetRot = GetRotationForDirection(targetDir);
                
                Vector3 euler = targetRot.eulerAngles;
                fishTransform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            }
        }
    }
    
    public Vector3 GetPosition() => fishTransform.position;
    public Vector3 GetForward() => fishTransform.forward;
    public float GetSpeed() => moveSpeed;
}