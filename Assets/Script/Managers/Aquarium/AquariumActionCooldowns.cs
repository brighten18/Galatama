using System.Collections.Generic;
using UnityEngine;

public class AquariumActionCooldowns : MonoBehaviour
{
    private readonly Dictionary<string, float> readyTimes = new Dictionary<string, float>();

    public bool IsReady(string key)
    {
        return string.IsNullOrEmpty(key) ||
               !readyTimes.TryGetValue(key, out float readyTime) ||
               Time.time >= readyTime;
    }

    public float GetRemaining(string key)
    {
        if (string.IsNullOrEmpty(key) || !readyTimes.TryGetValue(key, out float readyTime))
            return 0f;

        return Mathf.Max(0f, readyTime - Time.time);
    }

    public void StartCooldown(string key, float cooldownSeconds)
    {
        if (string.IsNullOrEmpty(key) || cooldownSeconds <= 0f)
            return;

        readyTimes[key] = Time.time + cooldownSeconds;
    }
}
