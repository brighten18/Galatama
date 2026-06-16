using System.Collections;
using Cinemachine;
using GALATAMA.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GALATAMA.Cutscene
{
    /// <summary>
    /// Memainkan intro cutscene berbasis Cinemachine dengan perpindahan kamera per area.
    /// Setiap sequence hanya dimainkan sekali per save slot.
    /// </summary>
    public class CutsceneController : MonoBehaviour
    {
        [System.Serializable]
        public class ShotData
        {
            public string shotName = "Shot";
            public CinemachineVirtualCamera virtualCamera;
            [Min(0.1f)] public float duration = 3f;
        }

        [Header("Sequence")]
        [SerializeField] private ShotData[] shots;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Galatama";

        [Header("UI")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Text shotLabelText;
        [SerializeField] private Image fadeOverlay;

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float initialDelay = 0.35f;

        [Header("Camera Priority")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 0;

        private bool isTransitioning;
        private Coroutine playRoutine;

        private void Awake()
        {
            SetAllCameraPriority(inactivePriority);
            SetFadeAlpha(1f);
        }

        private void Start()
        {
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipCutscene);

            playRoutine = StartCoroutine(PlaySequence());
        }

        private void OnDestroy()
        {
            if (skipButton != null)
                skipButton.onClick.RemoveListener(SkipCutscene);
        }

        private IEnumerator PlaySequence()
        {
            if (shots == null || shots.Length == 0)
            {
                yield return LoadGameplay();
                yield break;
            }

            yield return new WaitForSeconds(initialDelay);
            yield return Fade(1f, 0f);

            for (int i = 0; i < shots.Length; i++)
            {
                ShotData shot = shots[i];
                ActivateShot(shot);
                yield return new WaitForSeconds(Mathf.Max(0.1f, shot != null ? shot.duration : 0.1f));
            }

            yield return LoadGameplay();
        }

        private void ActivateShot(ShotData shot)
        {
            SetAllCameraPriority(inactivePriority);

            if (shot == null)
                return;

            if (shot.virtualCamera != null)
                shot.virtualCamera.Priority = activePriority;

            if (shotLabelText != null)
                shotLabelText.text = string.IsNullOrWhiteSpace(shot.shotName) ? string.Empty : shot.shotName;
        }

        private void SkipCutscene()
        {
            if (isTransitioning)
                return;

            if (playRoutine != null)
                StopCoroutine(playRoutine);

            StartCoroutine(LoadGameplay());
        }

        private IEnumerator LoadGameplay()
        {
            if (isTransitioning)
                yield break;

            isTransitioning = true;

            if (skipButton != null)
                skipButton.interactable = false;

            MarkCutsceneAsPlayed();
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void MarkCutsceneAsPlayed()
        {
            int activeSlot = SaveGameService.GetActiveSlotIndex();
            if (!SaveGameService.IsValidSlotIndex(activeSlot))
                return;

            SaveGameService.MarkIntroCutscenePlayed(activeSlot);
        }

        private void SetAllCameraPriority(int priority)
        {
            if (shots == null)
                return;

            for (int i = 0; i < shots.Length; i++)
            {
                ShotData shot = shots[i];
                if (shot != null && shot.virtualCamera != null)
                    shot.virtualCamera.Priority = priority;
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeOverlay == null)
                yield break;

            float elapsed = 0f;
            Color color = fadeOverlay.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
                fadeOverlay.color = color;
                yield return null;
            }

            color.a = to;
            fadeOverlay.color = color;
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeOverlay == null)
                return;

            Color color = fadeOverlay.color;
            color.a = alpha;
            fadeOverlay.color = color;
        }
    }
}
