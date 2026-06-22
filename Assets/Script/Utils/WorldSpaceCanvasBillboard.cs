using UnityEngine;

[ExecuteAlways]
public class WorldSpaceCanvasBillboard : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] private Transform anchorTarget;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool useMainCamera = true;

    [Header("Rotation")]
    [SerializeField] private bool lockXAxis = false;
    [SerializeField] private bool lockZAxis = false;
    [SerializeField] private bool flipForward = false;

    private void LateUpdate()
    {
        Transform anchor = anchorTarget != null ? anchorTarget : transform.parent;
        if (anchor != null)
            transform.position = anchor.position + worldOffset;

        Camera cam = ResolveCamera();
        if (cam == null)
            return;

        Vector3 direction = transform.position - cam.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Vector3 euler = rotation.eulerAngles;

        if (lockXAxis)
            euler.x = 0f;

        if (lockZAxis)
            euler.z = 0f;

        transform.rotation = Quaternion.Euler(euler);

        if (flipForward)
            transform.Rotate(0f, 180f, 0f, Space.Self);
    }

    public void SetAnchor(Transform newAnchor)
    {
        anchorTarget = newAnchor;
    }

    public void SetOffset(Vector3 newOffset)
    {
        worldOffset = newOffset;
    }

    private Camera ResolveCamera()
    {
        if (targetCamera != null)
            return targetCamera;

        if (!useMainCamera)
            return null;

        return Camera.main;
    }
}
