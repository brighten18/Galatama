using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class QuizRewardUnlockManager : MonoBehaviour
{
    [Serializable]
    public class RewardDisplayData
    {
        public string rewardTitle;
        [TextArea(2, 4)] public string rewardDescription;
        public Sprite rewardIcon;

        public bool HasVisibleContent()
        {
            return !string.IsNullOrWhiteSpace(rewardTitle)
                || !string.IsNullOrWhiteSpace(rewardDescription)
                || rewardIcon != null;
        }
    }

    [Serializable]
    public class WaveInteractionReward
    {
        [Min(1)] public int requiredWaveNumber = 1;

        [Header("Interaction Lock")]
        [Tooltip("Collider blocker tanpa mesh. Aktif saat reward masih terkunci.")]
        public GameObject[] interactionBlockers;
        [Tooltip("Opsional: komponen interaksi yang juga dikunci/dibuka.")]
        public Behaviour[] behavioursToUnlock;

        [Header("Lock Visual")]
        [Tooltip("Prefab 3D gembok yang akan di-spawn di anchor.")]
        public GameObject lockVisualPrefab;
        [Tooltip("Titik spawn gembok. Buat child empty object agar posisinya presisi.")]
        public Transform[] lockVisualAnchors;
        [Tooltip("Parent optional untuk instance gembok. Kosongkan jika ingin parent ke anchor.")]
        public Transform lockVisualParent;

        [Header("Aquarium Freeze")]
        [Tooltip("Aquarium reward yang harus dibekukan total sebelum reward dibuka.")]
        public AquariumSystem[] aquariumsToFreeze;

        [Header("Reward UI")]
        [Tooltip("Data hadiah yang ditampilkan ke player saat wave ini lulus.")]
        public RewardDisplayData rewardDisplay;

        [NonSerialized] public readonly List<GameObject> spawnedLockVisuals = new List<GameObject>();
    }

    [SerializeField] private WaveInteractionReward[] waveRewards;

    // Digunakan untuk memastikan OnEnable tidak dijalankan sebelum Start()
    // menyelesaikan inisialisasi awal (menghindari race condition Awake order).
    private bool hasStarted = false;

    private void Start()
    {
        hasStarted = true;
        LogSavedWaveState();
        RefreshRewardsFromSave();
    }

    /// <summary>
    /// Menampilkan state PlayerPrefs setiap wave saat game mulai.
    /// Berguna untuk mendeteksi data lama dari sesi testing sebelumnya.
    /// </summary>
    private void LogSavedWaveState()
    {
        if (waveRewards == null || waveRewards.Length == 0) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[QuizReward] State PlayerPrefs saat Start():");
        for (int i = 0; i < waveRewards.Length; i++)
        {
            WaveInteractionReward r = waveRewards[i];
            if (r == null) continue;
            bool passed = IsWavePassed(r.requiredWaveNumber);
            sb.AppendLine($"  Wave {r.requiredWaveNumber}: {(passed ? "SUDAH LULUS → reward TERBUKA" : "belum lulus → reward TERKUNCI")}");
        }
        sb.Append("  [INFO] Jika wave terlihat 'SUDAH LULUS' padahal belum, ");
        sb.Append("klik kanan komponen QuizManager di Inspector → 'DEBUG: Reset All Quiz Progress'");
        Debug.Log(sb.ToString());
    }

    private void OnEnable()
    {
        // Hanya refresh saat komponen di-enable setelah Start() selesai.
        // Tidak dijalankan saat Awake/OnEnable pertama kali karena AquariumSystem
        // (DefaultExecutionOrder -50) belum menyelesaikan Awake-nya saat ini
        // (QuizRewardUnlockManager memiliki DefaultExecutionOrder -200 — lebih awal).
        if (hasStarted)
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
            SetBlockersActive(reward.interactionBlockers, !unlocked);
            SetBehavioursEnabled(reward.behavioursToUnlock, unlocked);
            SyncLockVisuals(reward, !unlocked);
            SetAquariumUnlockState(reward.aquariumsToFreeze, unlocked);
        }
    }

    public bool TryGetRewardDisplayData(int waveNumber, out RewardDisplayData rewardDisplay)
    {
        rewardDisplay = null;

        if (waveRewards == null)
            return false;

        for (int i = 0; i < waveRewards.Length; i++)
        {
            WaveInteractionReward reward = waveRewards[i];
            if (reward == null || reward.requiredWaveNumber != waveNumber)
                continue;

            if (reward.rewardDisplay == null || !reward.rewardDisplay.HasVisibleContent())
                return false;

            rewardDisplay = reward.rewardDisplay;
            return true;
        }

        return false;
    }

    private bool IsWavePassed(int waveNumber)
    {
        return QuizManager.Instance != null && QuizManager.Instance.IsWavePassed(waveNumber);
    }

    private void SetBlockersActive(GameObject[] blockers, bool active)
    {
        if (blockers == null) return;
        for (int i = 0; i < blockers.Length; i++)
        {
            if (blockers[i] != null)
                blockers[i].SetActive(active);
        }
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

    private void SyncLockVisuals(WaveInteractionReward reward, bool shouldShow)
    {
        if (reward == null) return;

        if (!shouldShow)
        {
            ClearSpawnedLockVisuals(reward);
            return;
        }

        if (reward.lockVisualPrefab == null || reward.lockVisualAnchors == null || reward.lockVisualAnchors.Length == 0)
            return;

        if (reward.spawnedLockVisuals.Count == reward.lockVisualAnchors.Length)
        {
            for (int i = 0; i < reward.spawnedLockVisuals.Count; i++)
            {
                if (reward.spawnedLockVisuals[i] == null)
                {
                    ClearSpawnedLockVisuals(reward);
                    break;
                }
            }
        }

        if (reward.spawnedLockVisuals.Count > 0)
            return;

        for (int i = 0; i < reward.lockVisualAnchors.Length; i++)
        {
            Transform anchor = reward.lockVisualAnchors[i];
            if (anchor == null) continue;

            Transform parent = reward.lockVisualParent != null ? reward.lockVisualParent : anchor;
            GameObject instance = Instantiate(reward.lockVisualPrefab, anchor.position, anchor.rotation, parent);
            if (reward.lockVisualParent == null)
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }

            reward.spawnedLockVisuals.Add(instance);
        }
    }

    private void ClearSpawnedLockVisuals(WaveInteractionReward reward)
    {
        for (int i = 0; i < reward.spawnedLockVisuals.Count; i++)
        {
            if (reward.spawnedLockVisuals[i] != null)
                Destroy(reward.spawnedLockVisuals[i]);
        }

        reward.spawnedLockVisuals.Clear();
    }

    private void SetAquariumUnlockState(AquariumSystem[] aquariums, bool unlocked)
    {
        if (aquariums == null) return;
        for (int i = 0; i < aquariums.Length; i++)
        {
            if (aquariums[i] != null)
                aquariums[i].SetRewardUnlocked(unlocked);
        }
    }
}
