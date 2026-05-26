using System;
using UnityEngine;

public class QuizRewardUnlockManager : MonoBehaviour
{
    [Serializable]
    public class WaveInteractionReward
    {
        [Min(1)] public int requiredWaveNumber = 1;
        [Tooltip("Komponen interaksi yang akan dibuka setelah wave ini lulus.")]
        public Behaviour[] behavioursToUnlock;
    }

    [SerializeField] private WaveInteractionReward[] waveRewards;

    private const string KeyWavePassedPrefix = "QUIZ_WAVE_PASSED_";

    private void Start()
    {
        RefreshRewardsFromSave();
    }

    public void OnWavePassed(int waveNumber)
    {
        if (waveNumber <= 0) return;
        RefreshRewardsFromSave();
    }

    public void RefreshRewardsFromSave()
    {
        if (waveRewards == null) return;

        for (int i = 0; i < waveRewards.Length; i++)
        {
            WaveInteractionReward reward = waveRewards[i];
            if (reward == null) continue;

            bool unlocked = IsWavePassed(reward.requiredWaveNumber);
            SetBehavioursEnabled(reward.behavioursToUnlock, unlocked);
        }
    }

    private bool IsWavePassed(int waveNumber)
    {
        return PlayerPrefs.GetInt(KeyWavePassedPrefix + waveNumber, 0) == 1;
    }

    private void SetBehavioursEnabled(Behaviour[] behaviours, bool enabled)
    {
        if (behaviours == null) return;
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = enabled;
        }
    }
}
