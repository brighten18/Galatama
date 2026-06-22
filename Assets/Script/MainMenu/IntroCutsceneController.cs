using UnityEngine;
using UnityEngine.SceneManagement;

namespace GALATAMA.MainMenu
{
    public class IntroCutsceneController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "Galatama";
        [SerializeField] private bool useLoadingScene = false;
        [SerializeField] private string loadingSceneName = "Loading";

        public void FinishIntroCutscene()
        {
            int activeSlot = SaveGameService.GetActiveSlotIndex();
            if (SaveGameService.IsValidSlotIndex(activeSlot))
                SaveGameService.MarkIntroCutscenePlayed(activeSlot);

            if (useLoadingScene && !string.IsNullOrWhiteSpace(loadingSceneName))
            {
                SaveGameService.SetPendingTargetScene(gameplaySceneName);
                SceneManager.LoadScene(loadingSceneName);
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
