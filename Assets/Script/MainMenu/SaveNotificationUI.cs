using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class SaveNotificationUI : MonoBehaviour
    {
        private sealed class CoroutineRunner : MonoBehaviour
        {
        }

        private static CoroutineRunner runner;

        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text messageText;
        [SerializeField] private string successMessage = "Progress berhasil disimpan";
        [SerializeField] private string failedMessage = "Gagal menyimpan progress";
        [SerializeField] private float displayDuration = 2f;

        private Coroutine hideRoutine;
        private bool initialized;

        public float DisplayDuration
        {
            get { return displayDuration; }
            set { displayDuration = Mathf.Max(0f, value); }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public void ShowSaveResult(bool success)
        {
            string message = success ? successMessage : failedMessage;
            ShowMessage(message, displayDuration);
        }

        public void ShowMessage(string message, float duration)
        {
            EnsureInitialized();

            if (messageText != null)
                messageText.text = message;

            popupRoot.SetActive(true);

            CoroutineRunner coroutineRunner = GetOrCreateRunner();
            if (coroutineRunner == null)
            {
                Debug.LogError("[SaveNotificationUI] Coroutine runner tidak tersedia.");
                return;
            }

            if (hideRoutine != null)
                coroutineRunner.StopCoroutine(hideRoutine);

            hideRoutine = coroutineRunner.StartCoroutine(HideAfterDelay(Mathf.Max(0f, duration)));
        }

        private IEnumerator HideAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            if (popupRoot != null)
                popupRoot.SetActive(false);

            hideRoutine = null;
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            if (popupRoot == null)
                popupRoot = gameObject;

            popupRoot.SetActive(false);
            initialized = true;
        }

        private static CoroutineRunner GetOrCreateRunner()
        {
            if (runner != null)
                return runner;

            runner = FindFirstObjectByType<CoroutineRunner>();
            if (runner != null)
                return runner;

            GameObject runnerObject = new GameObject("__SaveNotificationRunner");
            DontDestroyOnLoad(runnerObject);
            runner = runnerObject.AddComponent<CoroutineRunner>();
            return runner;
        }
    }
}
