using UnityEngine;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class SaveSlotButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Text statusText;

        public Button Button => button;

        public void SetVisual(int slotIndex, SaveSlotHeaderData header)
        {
            if (titleText != null)
                titleText.text = header != null && header.exists && !string.IsNullOrWhiteSpace(header.saveName)
                    ? header.saveName
                    : "Slot " + slotIndex;

            if (statusText != null)
            {
                if (header == null || !header.exists)
                    statusText.text = "Slot Kosong";
                else if (!header.hasSavedProgress)
                    statusText.text = "Belum Disimpan";
                else
                    statusText.text = "Tersimpan";
            }

            if (detailText != null)
            {
                if (header == null || !header.exists)
                {
                    detailText.text = "Belum ada data.";
                    return;
                }

                string location = string.IsNullOrWhiteSpace(header.summaryLocationName) ? header.sceneName : header.summaryLocationName;
                if (header.savedAtTicks <= 0L)
                {
                    detailText.text = string.IsNullOrWhiteSpace(location)
                        ? "Save baru belum punya progress."
                        : location;
                    return;
                }

                System.DateTime savedTime = new System.DateTime(header.savedAtTicks, System.DateTimeKind.Utc).ToLocalTime();
                detailText.text = location + "\n" + savedTime.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
