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

    [Header("Terrain Avoidance")]
    [Tooltip("Jarak minimum ikan dari permukaan terrain")]
    [SerializeField] private float terrainOffset = 0.5f;
    [Tooltip("Jarak mulai menghindari terrain (steering mulai aktif)")]
    [SerializeField] private float terrainAvoidanceDistance = 2f;

    private Transform fishTransform;
    private Bounds zoneBounds;
    private Collider boundaryCollider;
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

        newPosition = ClampAboveTerrain(newPosition);
        fishTransform.position = hasBounds ? ClampToBounds(newPosition) : newPosition;
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f) return;

        // Untuk rotasi yaw (kiri-kanan) gunakan komponen horizontal saja
        // agar model tidak miring terlalu ekstrem
        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z);
        if (horizontalDir.sqrMagnitude <= 0.0001f) return;

        Quaternion targetRotation = GetRotationForDirection(horizontalDir.normalized);
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
        boundaryCollider = null;
        hasBounds = true;

        if (lockYPosition)
        {
            fixedYPosition = bounds.center.y;
        }

        fishTransform.position = ClampToBounds(fishTransform.position);
    }

    public void SetBoundary(Collider colliderBoundary)
    {
        if (colliderBoundary == null) return;

        boundaryCollider = colliderBoundary;
        zoneBounds = colliderBoundary.bounds;
        hasBounds = true;

        if (lockYPosition)
            fixedYPosition = colliderBoundary.bounds.center.y;

        fishTransform.position = ClampToBounds(fishTransform.position);
    }

    public Vector3 GetBoundarySteering()
    {
        if (!hasBounds) return Vector3.zero;
        if (boundaryCollider != null && boundaryCollider.enabled)
            return GetColliderBoundarySteering();

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

    private Vector3 GetColliderBoundarySteering()
    {
        Vector3 pos = fishTransform.position;
        Vector3 closest = boundaryCollider.ClosestPoint(pos);
        Vector3 toClosest = closest - pos;

        if (toClosest.sqrMagnitude > 0.0001f)
            return toClosest.normalized * 2f;

        Vector3 ahead = pos + fishTransform.forward * Mathf.Max(0.05f, boundaryTurnDistance);
        Vector3 closestAhead = boundaryCollider.ClosestPoint(ahead);
        Vector3 pullBack = closestAhead - ahead;

        if (lockYPosition)
            pullBack.y = 0f;

        return pullBack;
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
        if (boundaryCollider != null && boundaryCollider.enabled)
            return ClampToCollider(position);

        Vector3 min = GetSafeMin();
        Vector3 max = GetSafeMax();

        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.z = Mathf.Clamp(position.z, min.z, max.z);
        position.y = lockYPosition ? fixedYPosition : Mathf.Clamp(position.y, min.y, max.y);

        return position;
    }

    private Vector3 ClampToCollider(Vector3 position)
    {
        Vector3 closest = boundaryCollider.ClosestPoint(position);
        Vector3 result = position;

        if ((closest - position).sqrMagnitude > 0.0001f)
            result = closest;

        if (lockYPosition)
            result.y = fixedYPosition;

        return result;
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

    // ─── Terrain Avoidance ──────────────────────────────────────────────────

    /// <summary>
    /// Kembalikan gaya steering ke atas saat ikan terlalu dekat dengan terrain.
    /// Dipanggil oleh FishBrain dan digabungkan ke final direction.
    /// </summary>
    public Vector3 GetTerrainAvoidanceSteering()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return Vector3.zero;

        Vector3 pos = fishTransform.position;
        float terrainWorldY = terrain.SampleHeight(pos) + terrain.transform.position.y;
        float distanceAboveTerrain = pos.y - terrainWorldY;

        if (distanceAboveTerrain >= terrainAvoidanceDistance) return Vector3.zero;

        float strength = 1f - Mathf.Clamp01(distanceAboveTerrain / Mathf.Max(0.01f, terrainAvoidanceDistance));
        return Vector3.up * strength;
    }

    /// <summary>
    /// Pastikan posisi tidak berada di bawah permukaan terrain.
    /// Hard clamp sebagai failsafe terakhir.
    /// </summary>
    private Vector3 ClampAboveTerrain(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return position;

        float terrainWorldY = terrain.SampleHeight(position) + terrain.transform.position.y;
        float minY = terrainWorldY + terrainOffset;

        if (position.y < minY)
            position.y = minY;

        return position;
    }
}
