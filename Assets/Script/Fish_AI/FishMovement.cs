// Scripts/Fish/Core/FishMovement.cs

using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float acceleration = 2.0f;

    [Header("Model Orientation")]
    [SerializeField] private ForwardDirection modelForward = ForwardDirection.Z_Positive;

    [Header("Boundary Settings")]
    [SerializeField] private bool enforceBoundary = true;
    [SerializeField] private float boundaryPadding = 1f;

    private Transform fishTransform;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;

    private bool hasBoundary = false;
    private Bounds currentBounds;

    void Awake()
    {
        fishTransform = transform;
    }

    void Update()
    {
        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            Time.deltaTime * acceleration
        );

        if (enforceBoundary && hasBoundary)
        {
            EnforceBoundary();
        }
    }

    public void MoveToPoint(Vector3 targetPosition, float speed)
    {
        targetSpeed = speed;

        Vector3 direction = targetPosition - fishTransform.position;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        direction.Normalize();

        RotateTowards(direction);

        Vector3 movement = direction * currentSpeed * Time.deltaTime;
        fishTransform.position += movement;
    }

    public void MoveInDirection(Vector3 direction, float speed)
    {
        targetSpeed = speed;

        if (direction.sqrMagnitude <= 0.01f)
        {
            return;
        }

        direction.Normalize();

        RotateTowards(direction);

        Vector3 movement = direction * currentSpeed * Time.deltaTime;
        fishTransform.position += movement;
    }

    public void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.01f) return;

        Quaternion targetRotation = GetRotationForDirection(direction);

        fishTransform.rotation = Quaternion.RotateTowards(
            fishTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private Quaternion GetRotationForDirection(Vector3 worldDirection)
    {
        Vector3 modelForwardVector = GetModelForwardVector();

        Quaternion lookRotation = Quaternion.LookRotation(worldDirection);
        Quaternion offsetRotation = Quaternion.FromToRotation(Vector3.forward, modelForwardVector);

        return lookRotation * Quaternion.Inverse(offsetRotation);
    }

    private Vector3 GetModelForwardVector()
    {
        switch (modelForward)
        {
            case ForwardDirection.Z_Positive:
                return Vector3.forward;

            case ForwardDirection.Z_Negative:
                return Vector3.back;

            case ForwardDirection.X_Positive:
                return Vector3.right;

            case ForwardDirection.X_Negative:
                return Vector3.left;

            case ForwardDirection.Y_Positive:
                return Vector3.up;

            case ForwardDirection.Y_Negative:
                return Vector3.down;

            default:
                return Vector3.forward;
        }
    }

    public void SetBoundary(Bounds bounds)
    {
        currentBounds = bounds;
        hasBoundary = true;

        Debug.Log($"[FishMovement] Boundary set: {bounds.center}, Size: {bounds.size}");
    }

    public void ClearBoundary()
    {
        hasBoundary = false;
    }

    public Bounds GetBoundary()
    {
        return currentBounds;
    }

    public bool HasBoundary()
    {
        return hasBoundary;
    }

    private void EnforceBoundary()
    {
        Vector3 clampedPosition = ClampPointInsideBoundary(fishTransform.position);

        if (clampedPosition != fishTransform.position)
        {
            fishTransform.position = clampedPosition;

            Vector3 directionToCenter = GetDirectionAwayFromBoundary();
            RotateTowards(directionToCenter);

            Debug.LogWarning($"[FishMovement] Fish clamped inside bounds: {clampedPosition}");
        }
    }

    public Vector3 ClampPointInsideBoundary(Vector3 point)
    {
        if (!hasBoundary) return point;

        Vector3 min = currentBounds.min + Vector3.one * boundaryPadding;
        Vector3 max = currentBounds.max - Vector3.one * boundaryPadding;

        point.x = Mathf.Clamp(point.x, min.x, max.x);
        point.y = Mathf.Clamp(point.y, min.y, max.y);
        point.z = Mathf.Clamp(point.z, min.z, max.z);

        return point;
    }

    public bool IsNearBoundary(float threshold)
    {
        if (!hasBoundary) return false;

        Vector3 pos = fishTransform.position;

        Vector3 min = currentBounds.min + Vector3.one * boundaryPadding;
        Vector3 max = currentBounds.max - Vector3.one * boundaryPadding;

        float distToMinX = pos.x - min.x;
        float distToMaxX = max.x - pos.x;

        float distToMinY = pos.y - min.y;
        float distToMaxY = max.y - pos.y;

        float distToMinZ = pos.z - min.z;
        float distToMaxZ = max.z - pos.z;

        float minDistance = Mathf.Min(
            distToMinX,
            distToMaxX,
            distToMinY,
            distToMaxY,
            distToMinZ,
            distToMaxZ
        );

        return minDistance < threshold;
    }

    public Vector3 GetDirectionAwayFromBoundary()
    {
        if (!hasBoundary) return Vector3.zero;

        return (currentBounds.center - fishTransform.position).normalized;
    }

    public Vector3 GetRandomWanderOffset(float radius)
    {
        return new Vector3(
            Random.Range(-radius, radius),
            Random.Range(-radius * 0.3f, radius * 0.3f),
            Random.Range(-radius, radius)
        );
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

    public void SetModelForward(ForwardDirection direction)
    {
        modelForward = direction;
    }
}