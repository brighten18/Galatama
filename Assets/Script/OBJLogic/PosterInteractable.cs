using UnityEngine;

public class PosterInteractable : InteractableObject
{
    [SerializeField] private PosterData posterData;
    [SerializeField] private string fallbackPosterName = "Poster";

    [Header("Mission (optional)")]
    [Tooltip("Index of the mission to complete on interaction. Set to -1 to disable.")]
    [SerializeField] private int completeMissionIndex = -1;

    private void Awake()
    {
        base.Awake();
        itemName = posterData != null && !string.IsNullOrEmpty(posterData.PosterName)
            ? posterData.PosterName
            : fallbackPosterName;
    }

    protected override void Update()
    {
        if (PosterPopupManager.Instance != null && PosterPopupManager.Instance.IsOpen)
            return;

        base.Update();
    }

    protected override void HandleInteract()
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.ResetInteractInput();

        if (PosterPopupManager.Instance == null)
        {
            Debug.LogError("[PosterInteractable] PosterPopupManager belum ada di scene.");
            return;
        }

        PosterPopupManager.Instance.OpenPoster(posterData);

        if (completeMissionIndex >= 0 && MissionManager.Instance != null)
            MissionManager.Instance.CompleteMission(completeMissionIndex);
    }

    public override string GetItemName()
    {
        return itemName;
    }
}
