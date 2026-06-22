using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GALATAMA.MainMenu
{
    public class LoadingSceneController : MonoBehaviour
    {
        [SerializeField] private string fallbackSceneName = "Galatama";
        [SerializeField] private float minimumLoadingDuration = 1f;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressText;
        [SerializeField] private Text statusText;
        [SerializeField] private string loadingMessage = "Memuat...";

        private void Start()
        {
            string targetSceneName = SaveGameService.ConsumePendingTargetScene();
            if (string.IsNullOrWhiteSpace(targetSceneName))
                targetSceneName = fallbackSceneName;

            if (statusText != null)
                statusText.text = loadingMessage;

            StartCoroutine(LoadTargetSceneRoutine(targetSceneName));
        }

        private IEnumerator LoadTargetSceneRoutine(string targetSceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            if (operation == null)
            {
                Debug.LogError("[LoadingScene] Gagal memulai loading scene: " + targetSceneName);
                yield break;
            }

            operation.allowSceneActivation = false;
            float elapsed = 0f;

            while (!operation.isDone)
            {
                elapsed += Time.deltaTime;
                float normalizedProgress = Mathf.Clamp01(operation.progress / 0.9f);

                if (progressSlider != null)
                    progressSlider.value = normalizedProgress;

                if (progressText != null)
                    progressText.text = Mathf.RoundToInt(normalizedProgress * 100f) + "%";

                if (operation.progress >= 0.9f && elapsed >= minimumLoadingDuration)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }
    }
}
