using System;
using System.Collections.Generic;
using UnityEngine;

public class RasFishManager : MonoBehaviour
{
    private const float FULL_TO_EMPTY_HUNGER_SECONDS = 300f;
    private const float HUNGER_LOSS_PER_SEC = 100f / FULL_TO_EMPTY_HUNGER_SECONDS;

    private WaterQualityState water;
    private RasWaterSimulator simulator;
    private List<FishInstanceState> fishList;

    private readonly Dictionary<string, FishDeathCountdown> doCountdowns
        = new Dictionary<string, FishDeathCountdown>();

    public event Action<string, string> OnFishDied;
    public event Action<string, float, DOStatus> OnCountdownUpdated;

    public void Initialize(
        WaterQualityState waterState,
        List<FishInstanceState> fish,
        RasWaterSimulator rasSimulator)
    {
        water = waterState;
        fishList = fish;
        simulator = rasSimulator;
    }

    public void Tick(float dt)
    {
        if (fishList == null || water == null || simulator == null) return;

        DOStatus doStatus = simulator.GetDOStatus();

        foreach (FishInstanceState fish in fishList)
        {
            if (fish == null || !fish.isAlive) continue;

            TickHunger(fish, dt);
            TickDOCountdown(fish, dt, doStatus);
            TickStress(fish, doStatus);
        }

        CleanupStaleCountdowns();
    }

    public void RegisterFish(FishInstanceState fish)
    {
        if (fish == null) return;
        fish.hunger = Mathf.Clamp(fish.hunger, 0f, fish.maxHunger);
    }

    public void UnregisterFish(FishInstanceState fish)
    {
        if (fish == null) return;
        doCountdowns.Remove(fish.instanceId);
    }

    private void TickHunger(FishInstanceState fish, float dt)
    {
        fish.hunger = Mathf.Max(0f, fish.hunger - HUNGER_LOSS_PER_SEC * dt);
        if (fish.hunger <= 0f)
            KillFish(fish, "tidak diberi makan selama 5 menit");
    }

    private void TickDOCountdown(FishInstanceState fish, float dt, DOStatus doStatus)
    {
        if (!doCountdowns.TryGetValue(fish.instanceId, out FishDeathCountdown countdown))
        {
            if (doStatus == DOStatus.Safe) return;

            countdown = new FishDeathCountdown(fish.instanceId);
            countdown.OnDeathTriggered += id => KillFishById(id, "DO rendah");
            countdown.OnCountdownUpdated += (id, remaining, phase) =>
                OnCountdownUpdated?.Invoke(id, remaining, phase);

            doCountdowns[fish.instanceId] = countdown;
        }

        countdown.Tick(doStatus, dt);

        if (!countdown.IsActive)
            doCountdowns.Remove(fish.instanceId);
    }

    private void TickStress(FishInstanceState fish, DOStatus doStatus)
    {
        fish.isStressed =
            water.salinity < 32f ||
            water.salinity > 35f ||
            water.temperature < 21f ||
            water.temperature > 29f ||
            water.ph < 6f ||
            water.ph > 8f ||
            doStatus != DOStatus.Safe;
    }

    private void KillFish(FishInstanceState fish, string reason)
    {
        if (fish == null || !fish.isAlive) return;

        fish.health = 0f;
        fish.isAlive = false;
        fish.isStressed = false;

        doCountdowns.Remove(fish.instanceId);

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

    private void CleanupStaleCountdowns()
    {
        if (fishList == null) return;

        var activeIds = new HashSet<string>();
        foreach (FishInstanceState fish in fishList)
        {
            if (fish != null && fish.isAlive)
                activeIds.Add(fish.instanceId);
        }

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
