using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mengelola kondisi per-ikan dalam simulasi RAS Galatama:
/// hunger, death countdown DO, stres salinitas, dan NH3 toksisitas.
///
/// Dipanggil tiap frame oleh AquariumSystem melalui Tick().
/// </summary>
public class RasFishManager : MonoBehaviour
{
    private const float HUNGER_CYCLE_SECONDS  = 300f;  // ikan lapar tiap 5 menit
    private const float HUNGER_FILL_PER_SEC   = 100f / HUNGER_CYCLE_SECONDS; // 0.333/s â†’ penuh dalam 5 menit
    private const float DEATH_BY_HUNGER_SECS  = 300f;  // 5 menit setelah lapar penuh â†’ mati

    private WaterQualityState     water;
    private RasWaterSimulator     simulator;
    private List<FishInstanceState> fishList;

    // Countdown kematian per ikan (keyed by instanceId)
    private readonly Dictionary<string, FishDeathCountdown> doCountdowns
        = new Dictionary<string, FishDeathCountdown>();

    // Timer kelaparan (keyed by instanceId) â€” dimulai saat hunger = maxHunger
    private readonly Dictionary<string, float> starvationTimers
        = new Dictionary<string, float>();

    /// <summary>Event: ikan mati (instanceId, sebab).</summary>
    public event Action<string, string> OnFishDied;

    /// <summary>Event: update countdown (instanceId, sisa detik, fase).</summary>
    public event Action<string, float, DOStatus> OnCountdownUpdated;

    // â”€â”€â”€ Inisialisasi â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    /// <summary>
    /// Inisialisasi dengan referensi ke state bersama dari AquariumSystem.
    /// </summary>
    public void Initialize(
        WaterQualityState waterState,
        List<FishInstanceState> fish,
        RasWaterSimulator rasSimulator)
    {
        water     = waterState;
        fishList  = fish;
        simulator = rasSimulator;
    }

    // â”€â”€â”€ Tick Utama â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€...

    /// <summary>
    /// Dipanggil tiap frame dari AquariumSystem.Update() dengan Time.deltaTime.
    /// </summary>
    public void Tick(float dt)
    {
        if (fishList == null || water == null) return;

        DOStatus doStatus         = simulator.GetDOStatus();
        bool     nh3Toxic         = simulator.IsNH3ToxicityDoubled();
        float    feedEfficiency   = simulator.GetFeedEfficiency();

        foreach (FishInstanceState fish in fishList)
        {
            if (fish == null || !fish.isAlive) continue;

            TickHunger(fish, dt, feedEfficiency);
            TickDOCountdown(fish, dt, doStatus, nh3Toxic);
            TickStress(fish);
        }

        // Bersihkan countdown untuk ikan yang sudah tidak ada
        CleanupStaleCountdowns();
    }

    /// <summary>
    /// Daftarkan ikan baru saat ditambahkan ke akuarium.
    /// </summary>
    public void RegisterFish(FishInstanceState fish)
    {
        if (fish == null) return;
        // Countdown dibuat lazily saat pertama kali DO berbahaya
    }

    /// <summary>
    /// Hapus countdown saat ikan dikeluarkan dari akuarium.
    /// </summary>
    public void UnregisterFish(FishInstanceState fish)
    {
        if (fish == null) return;
        doCountdowns.Remove(fish.instanceId);
        starvationTimers.Remove(fish.instanceId);
    }

    // â”€â”€â”€ Hunger â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    private void TickHunger(FishInstanceState fish, float dt, float feedEfficiency)
    {
        // Efektivitas pakan memengaruhi seberapa cepat hunger berkurang saat makan,
        // tapi untuk simulasi kelaparan, kita naikkan hunger sesuai cycle 5 menit.
        float hungerRate = HUNGER_FILL_PER_SEC * dt;
        fish.hunger = Mathf.Min(fish.maxHunger, fish.hunger + hungerRate);

        bool isStarving = fish.hunger >= fish.maxHunger;

        if (isStarving)
        {
            // Akumulasi timer kelaparan
            if (!starvationTimers.ContainsKey(fish.instanceId))
                starvationTimers[fish.instanceId] = 0f;

            starvationTimers[fish.instanceId] += dt;

            if (starvationTimers[fish.instanceId] >= DEATH_BY_HUNGER_SECS)
                KillFish(fish, "kelaparan");
        }
        else
        {
            // Reset timer begitu ikan makan (hunger turun)
            starvationTimers.Remove(fish.instanceId);
        }
    }

    // â”€â”€â”€ DO Countdown â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â...

    private void TickDOCountdown(
        FishInstanceState fish, float dt, DOStatus doStatus, bool nh3Toxic)
    {
        if (!doCountdowns.TryGetValue(fish.instanceId, out FishDeathCountdown countdown))
        {
            if (doStatus == DOStatus.Safe) return;

            countdown = new FishDeathCountdown(fish.instanceId);
            countdown.OnDeathTriggered   += id => KillFishById(id, "DO rendah");
            countdown.OnCountdownUpdated += (id, remaining, phase) =>
                OnCountdownUpdated?.Invoke(id, remaining, phase);

            doCountdowns[fish.instanceId] = countdown;
        }

        // NH3 toksik (pH>8.5) â†’ percepat countdown 2x
        float effectiveDt = nh3Toxic ? dt * 2f : dt;
        countdown.Tick(doStatus, effectiveDt);

        // Hapus countdown tidak aktif agar tidak menumpuk
        if (!countdown.IsActive)
            doCountdowns.Remove(fish.instanceId);
    }

    // â”€â”€â”€ Stres â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”...

    private void TickStress(FishInstanceState fish)
    {
        // Stres salinitas rendah (ditandai di FishInstanceState untuk dibaca AquariumSystem)
        bool salinityStress = water.salinity < 32f;
        fish.isStressed = salinityStress;
    }

    // â”€â”€â”€ Kematian â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”...

    private void KillFish(FishInstanceState fish, string reason)
    {
        if (!fish.isAlive) return;

        fish.health   = 0f;
        fish.isAlive  = false;
        fish.isStressed = false;

        doCountdowns.Remove(fish.instanceId);
        starvationTimers.Remove(fish.instanceId);

        Debug.Log($"[RasFishManager] Ikan '{fish.itemName}' ({fish.instanceId}) mati karena {reason}.");
        OnFishDied?.Invoke(fish.instanceId, reason);
    }

    private void KillFishById(string instanceId, string reason)
    {
        if (fishList == null) return;

        foreach (FishInstanceState fish in fishList)
        {
            if (fish != null && fish.instanceId == instanceId)
            {
                KillFish(fish, reason);
                return;
            }
        }
    }

    // â”€â”€â”€ Utilitas â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”...

    private void CleanupStaleCountdowns()
    {
        if (fishList == null) return;

        // Kumpulkan ID ikan yang masih hidup
        var activeIds = new HashSet<string>();
        foreach (FishInstanceState fish in fishList)
        {
            if (fish != null && fish.isAlive)
                activeIds.Add(fish.instanceId);
        }

        // Hapus countdown untuk ikan yang tidak ada lagi
        var toRemove = new List<string>();
        foreach (string id in doCountdowns.Keys)
        {
            if (!activeIds.Contains(id))
                toRemove.Add(id);
        }

        foreach (string id in toRemove)
            doCountdowns.Remove(id);
    }
}
