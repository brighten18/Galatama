using UnityEngine;

public class NotesInteractable : InteractableObject
{
    [Header("Mission")]
    [SerializeField] private int targetMissionIndex = 0;

    protected override void HandleInteract()
    {
        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.ResetInteractInput();

        if (MissionManager.Instance != null)
            MissionManager.Instance.CompleteMission(targetMissionIndex);

        Debug.Log($"[NotesInteractable] Player membaca: {GetItemName()}");
    }
}
