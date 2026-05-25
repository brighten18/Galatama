using UnityEngine;

/// <summary>
/// Membuat GameObject ini selalu mengikuti posisi dan rotasi dari tulang (bone) tertentu
/// yang ada di dalam Animator rig player.
///
/// Setup: Assign komponen Animator dan nama bone yang ingin diikuti di Inspector.
/// Contoh bone name untuk tangan kanan: "Bone.016"
/// </summary>
public class FollowBone : MonoBehaviour
{
    [Tooltip("Animator yang memiliki rig bertulang. Biasanya Animator di PlayerCapsule/armature.")]
    [SerializeField] private Animator rigAnimator;

    [Tooltip("Nama bone yang akan diikuti (case-sensitive, harus sama persis dengan nama bone di rig).")]
    [SerializeField] private string boneName = "Bone.016";

    [Tooltip("Offset posisi lokal relatif terhadap bone.")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;

    [Tooltip("Offset rotasi lokal relatif terhadap bone (Euler angles).")]
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;

    private Transform boneTransform;

    private void Start()
    {
        ResolveBone();
    }

    private void LateUpdate()
    {
        if (boneTransform == null)
        {
            ResolveBone();
            return;
        }

        transform.position = boneTransform.TransformPoint(localPositionOffset);
        transform.rotation = boneTransform.rotation * Quaternion.Euler(localRotationOffset);
    }

    /// <summary>
    /// Cari Transform bone berdasarkan nama di semua descendant Animator.
    /// </summary>
    private void ResolveBone()
    {
        if (rigAnimator == null)
        {
            // Coba cari Animator di parent PlayerCapsule
            rigAnimator = GetComponentInParent<Animator>();
        }

        if (rigAnimator == null)
        {
            Debug.LogWarning("[FollowBone] Animator tidak ditemukan. Assign di Inspector.");
            return;
        }

        boneTransform = FindBoneRecursive(rigAnimator.transform, boneName);

        if (boneTransform == null)
            Debug.LogWarning($"[FollowBone] Bone '{boneName}' tidak ditemukan dalam rig Animator.");
    }

    private static Transform FindBoneRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindBoneRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
