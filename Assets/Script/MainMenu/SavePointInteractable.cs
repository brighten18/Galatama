using UnityEngine;

namespace GALATAMA.MainMenu
{
    public class SavePointInteractable : InteractableObject
    {
        [SerializeField] private string savePointName = "Save Point";
        [SerializeField] private SaveNotificationUI saveNotificationUI;

        protected override void Awake()
        {
            base.Awake();
            itemName = savePointName;
        }

        protected override void HandleInteract()
        {
            if (PlayerInputManager.Instance != null)
                PlayerInputManager.Instance.ResetInteractInput();

            if (SaveGameRuntimeController.Instance == null)
            {
                Debug.LogError("[SavePoint] SaveGameRuntimeController tidak ditemukan di scene gameplay.");
                return;
            }

            bool saved = SaveGameRuntimeController.Instance.SaveActiveSlotFromScene();
            if (saveNotificationUI != null)
                saveNotificationUI.ShowSaveResult(saved);

            Debug.Log(saved
                ? "[SavePoint] Progress berhasil disimpan."
                : "[SavePoint] Gagal menyimpan progress.");
        }

        public override string GetItemName()
        {
            return savePointName;
        }
    }
}
