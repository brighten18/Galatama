using UnityEngine;

public class DoorInteractable : InteractableObject
{
    [System.Serializable]
    private class DoorLeaf
    {
        public Transform leaf;
        public Vector3 openEulerOffset = new Vector3(0f, 90f, 0f);
        [HideInInspector] public Quaternion closedLocalRotation;
    }

    [Header("Door Setup")]
    [SerializeField] private string displayName = "Pintu";
    [SerializeField] private DoorLeaf[] leaves;

    [Header("Animation")]
    [SerializeField, Min(1f)] private float rotateSpeedDegreesPerSecond = 180f;

    [Header("Audio")]
    [SerializeField] private AudioSource doorSfxSource;
    [SerializeField] private AudioClip openDoorSfx;
    [SerializeField] private AudioClip closeDoorSfx;
    [SerializeField, Range(0f, 1f)] private float doorSfxVolume = 1f;

    private bool isOpen;

    protected override void Awake()
    {
        base.Awake();
        itemName = displayName;
        CacheClosedRotations();
    }

    protected override void HandleInteract()
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.ResetInteractInput();

        isOpen = !isOpen;
        PlayDoorSfx(isOpen);
    }

    protected override void Update()
    {
        base.Update();
        AnimateLeaves();
    }

    private void CacheClosedRotations()
    {
        if (leaves == null) return;

        for (int i = 0; i < leaves.Length; i++)
        {
            if (leaves[i] == null || leaves[i].leaf == null) continue;
            leaves[i].closedLocalRotation = leaves[i].leaf.localRotation;
        }
    }

    private void AnimateLeaves()
    {
        if (leaves == null || leaves.Length == 0) return;

        float maxStep = rotateSpeedDegreesPerSecond * Time.deltaTime;
        for (int i = 0; i < leaves.Length; i++)
        {
            DoorLeaf doorLeaf = leaves[i];
            if (doorLeaf == null || doorLeaf.leaf == null) continue;

            Quaternion targetRotation = isOpen
                ? doorLeaf.closedLocalRotation * Quaternion.Euler(doorLeaf.openEulerOffset)
                : doorLeaf.closedLocalRotation;

            doorLeaf.leaf.localRotation = Quaternion.RotateTowards(
                doorLeaf.leaf.localRotation,
                targetRotation,
                maxStep
            );
        }
    }

    private void PlayDoorSfx(bool opening)
    {
        if (doorSfxSource == null)
            return;

        AudioClip clipToPlay = opening ? openDoorSfx : closeDoorSfx;
        if (clipToPlay == null)
            return;

        doorSfxSource.PlayOneShot(clipToPlay, Mathf.Clamp01(doorSfxVolume));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Pintu";

        if (rotateSpeedDegreesPerSecond < 1f)
            rotateSpeedDegreesPerSecond = 1f;

        itemName = displayName;
    }
#endif
}
