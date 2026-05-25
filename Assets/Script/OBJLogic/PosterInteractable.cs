using UnityEngine;

public class PosterInteractable : InteractableObject
{
    [SerializeField] private PosterData posterData;
    [SerializeField] private string fallbackPosterName = "Poster";

    private void Awake()
    {
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
    }

    public override string GetItemName()
    {
        return itemName;
    }
}
