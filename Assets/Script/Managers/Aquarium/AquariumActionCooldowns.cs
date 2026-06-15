using System.Collections.Generic;
using System;
using UnityEngine;
using GALATAMA.MainMenu;

public class AquariumActionCooldowns : MonoBehaviour
{
    private readonly Dictionary<string, long> readyTimesUtcTicks = new Dictionary<string, long>();

    public bool IsReady(string key)
    {
        return string.IsNullOrEmpty(key) ||
               !readyTimesUtcTicks.TryGetValue(key, out long readyAtTicks) ||
               DateTime.UtcNow.Ticks >= readyAtTicks;
    }

    public float GetRemaining(string key)
    {
        if (string.IsNullOrEmpty(key) || !readyTimesUtcTicks.TryGetValue(key, out long readyAtTicks))
            return 0f;

        double remainingSeconds = new TimeSpan(Math.Max(0L, readyAtTicks - DateTime.UtcNow.Ticks)).TotalSeconds;
        return Mathf.Max(0f, (float)remainingSeconds);
    }

    public void StartCooldown(string key, float cooldownSeconds)
    {
        if (string.IsNullOrEmpty(key) || cooldownSeconds <= 0f)
            return;

        long readyAtTicks = DateTime.UtcNow.AddSeconds(cooldownSeconds).Ticks;
        readyTimesUtcTicks[key] = readyAtTicks;
    }

    public List<CooldownEntrySaveData> CaptureSaveData()
    {
        List<CooldownEntrySaveData> result = new List<CooldownEntrySaveData>();
        long nowTicks = DateTime.UtcNow.Ticks;

        foreach (KeyValuePair<string, long> pair in readyTimesUtcTicks)
        {
            if (pair.Value <= nowTicks)
                continue;

            result.Add(new CooldownEntrySaveData
            {
                cooldownKey = pair.Key,
                nextReadyAtUtcTicks = pair.Value
            });
        }

        return result;
    }

    public void RestoreSaveData(List<CooldownEntrySaveData> entries)
    {
        readyTimesUtcTicks.Clear();
        if (entries == null)
            return;

        long nowTicks = DateTime.UtcNow.Ticks;
        for (int i = 0; i < entries.Count; i++)
        {
            CooldownEntrySaveData entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.cooldownKey) || entry.nextReadyAtUtcTicks <= nowTicks)
                continue;

            readyTimesUtcTicks[entry.cooldownKey] = entry.nextReadyAtUtcTicks;
        }
    }
}
