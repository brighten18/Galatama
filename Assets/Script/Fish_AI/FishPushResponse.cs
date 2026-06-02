using UnityEngine;

/// <summary>
/// Handles push impulses applied to a fish from external sources (e.g., player collision).
/// Works additively with FishMovement without requiring a Rigidbody.
/// </summary>
public class FishPushResponse : MonoBehaviour
{
    [Header("Push Settings")]
    [SerializeField] private float drag = 6f;
    [SerializeField] private float maxPushSpeed = 10f;

    private Vector3 _pushVelocity = Vector3.zero;

    /// <summary>Applies an impulse push force to the fish.</summary>
    public void ApplyPush(Vector3 impulse)
    {
        _pushVelocity += impulse;
        _pushVelocity = Vector3.ClampMagnitude(_pushVelocity, maxPushSpeed);
    }

    private void Update()
    {
        if (_pushVelocity.sqrMagnitude <= 0.0001f) return;

        transform.position += _pushVelocity * Time.deltaTime;
        _pushVelocity = Vector3.MoveTowards(_pushVelocity, Vector3.zero, drag * Time.deltaTime);
    }
}
