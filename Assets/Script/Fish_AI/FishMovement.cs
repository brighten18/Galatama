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
    [SerializeField] private float boundaryTurnDistance = 3f;
    [SerializeField] private float boundarySteerWeight = 4f;
    [SerializeField] private bool lockYPosition = false;
    [SerializeField] private float fixedYPosition = 5f;

    private Transform fishTransform;
    private Bounds zoneBounds;
    private bool hasBounds = false;

    void Awake()
    {
        fishTransform = transform;
    }

    public void Move(Vector3 direction, float speedMultiplier = 1f)
    {
        if (direction.sqrMagnitude <= 0.0001f) return;

        direction = direction.normalized;

        if (lockYPosition)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) return;
            direction.Normalize();
        }

        if (hasBounds)
        {
            Vector3 boundarySteering = GetBoundarySteering();
            if (boundarySteering.sqrMagnitude > 0.0001f)
            {
                direction = (direction + boundarySteering.normalized * boundarySteerWeight).normalized;
            }
        }

        RotateTowards(direction);

        Vector3 newPosition = fishTransform.position + direction * moveSpeed * speedMultiplier * Time.deltaTime;
        if (lockYPosition)
        {
            newPosition.y = fixedYPosition;
        }

        fishTransform.position = hasBounds ? ClampToBounds(newPosition) : newPosition;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f) return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRotation = GetRotationForDirection(direction.normalized);
        Vector3 currentEuler = fishTransform.rotation.eulerAngles;
        Vector3 targetEuler = targetRotation.eulerAngles;
        float newYRotation = Mathf.LerpAngle(currentEuler.y, targetEuler.y, rotationSpeed * Time.deltaTime / 120f);

        fishTransform.rotation = Quaternion.Euler(0f, newYRotation, 0f);
    }

    private Quaternion GetRotationForDirection(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude <= 0.0001f) return fishTransform.rotation;

        Quaternion lookRotation = Quaternion.LookRotation(worldDirection.normalized);
        Quaternion offsetRotation = Quaternion.FromToRotation(Vector3.forward, GetModelForwardVector());
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

        if (lockYPosition)
        {
            fixedYPosition = bounds.center.y;
        }

        fishTransform.position = ClampToBounds(fishTransform.position);
    }

    public Vector3 GetBoundarySteering()
    {
        if (!hasBounds) return Vector3.zero;

        Vector3 pos = fishTransform.position;
        Vector3 min = GetSafeMin();
        Vector3 max = GetSafeMax();
        Vector3 steer = Vector3.zero;

        AddAxisSteering(pos.x, min.x, max.x, Vector3.right, ref steer);
        AddAxisSteering(pos.z, min.z, max.z, Vector3.forward, ref steer);

        if (!lockYPosition)
        {
            AddAxisSteering(pos.y, min.y, max.y, Vector3.up, ref steer);
        }

        return steer;
    }

    private void AddAxisSteering(float value, float min, float max, Vector3 axis, ref Vector3 steer)
    {
        float safeTurnDistance = Mathf.Max(0.01f, boundaryTurnDistance);

        if (value < min)
        {
            steer += axis * 2f;
            return;
        }

        if (value > max)
        {
            steer -= axis * 2f;
            return;
        }

        float distanceToMin = value - min;
        if (distanceToMin < safeTurnDistance)
        {
            steer += axis * (1f - distanceToMin / safeTurnDistance);
        }

        float distanceToMax = max - value;
        if (distanceToMax < safeTurnDistance)
        {
            steer -= axis * (1f - distanceToMax / safeTurnDistance);
        }
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        Vector3 min = GetSafeMin();
        Vector3 max = GetSafeMax();

        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.z = Mathf.Clamp(position.z, min.z, max.z);
        position.y = lockYPosition ? fixedYPosition : Mathf.Clamp(position.y, min.y, max.y);

        return position;
    }

    private Vector3 GetSafeMin()
    {
        Vector3 padding = GetSafePadding();
        return zoneBounds.min + padding;
    }

    private Vector3 GetSafeMax()
    {
        Vector3 padding = GetSafePadding();
        return zoneBounds.max - padding;
    }

    private Vector3 GetSafePadding()
    {
        Vector3 extents = zoneBounds.extents;
        return new Vector3(
            Mathf.Min(boundaryPadding, Mathf.Max(0f, extents.x - 0.01f)),
            Mathf.Min(boundaryPadding, Mathf.Max(0f, extents.y - 0.01f)),
            Mathf.Min(boundaryPadding, Mathf.Max(0f, extents.z - 0.01f))
        );
    }

    public bool HasBoundary => hasBounds;
    public Vector3 GetPosition() => fishTransform.position;
    public Vector3 GetForward() => fishTransform.forward;
    public float GetSpeed() => moveSpeed;
}