using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class SaveNotificationUI : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text messageText;
        [SerializeField] private string successMessage = "Progress berhasil disimpan";
        [SerializeField] private string failedMessage = "Gagal menyimpan progress";
        [SerializeField] private float displayDuration = 2f;

        private Coroutine hideRoutine;

        public float DisplayDuration
        {
            get { return displayDuration; }
            set { displayDuration = Mathf.Max(0f, value); }
        }

        private void Awake()
        {
            if (popupRoot == null)
                popupRoot = gameObject;

            popupRoot.SetActive(false);
        }

        public void ShowSaveResult(bool success)
        {
            string message = success ? successMessage : failedMessage;
            ShowMessage(message, displayDuration);
        }

        public void ShowMessage(string message, float duration)
        {
            if (popupRoot == null)
                popupRoot = gameObject;

            if (messageText != null)
                messageText.text = message;

            popupRoot.SetActive(true);

            if (hideRoutine != null)
                StopCoroutine(hideRoutine);

            hideRoutine = StartCoroutine(HideAfterDelay(Mathf.Max(0f, duration)));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (popupRoot != null)
                popupRoot.SetActive(false);

            hideRoutine = null;
        }
    }
}
