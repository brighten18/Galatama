using System;
using UnityEngine;

public class FishDeathCountdown
{
    private const float DANGER_DURATION = 240f;
    private const float CRITICAL_DURATION = 60f;
    private const float ZERO_DURATION = 30f;

    private readonly string fishInstanceId;

    private bool isActive;
    private float remainingSeconds;
    private DOStatus currentPhase;

    public event Action<string> OnDeathTriggered;
    public event Action<string, float, DOStatus> OnCountdownUpdated;

    public bool IsActive => isActive;
    public float RemainingSeconds => remainingSeconds;
    public DOStatus CurrentPhase => currentPhase;

    public FishDeathCountdown(string instanceId)
    {
        fishInstanceId = instanceId;
    }

    public void Tick(DOStatus doStatus, float dt)
    {
        switch (doStatus)
        {
            case DOStatus.Safe:
                Cancel();
                return;
            case DOStatus.Danger:
                HandleDangerTick(dt);
                break;
            case DOStatus.Critical:
                HandleCriticalTick(dt);
                break;
            case DOStatus.Zero:
                HandleZeroTick(dt);
                break;
        }
    }

    public void Cancel()
    {
        if (!isActive) return;

        isActive = false;
        remainingSeconds = 0f;
        Debug.Log($"[DeathCountdown] {fishInstanceId}: Countdown dibatalkan, DO kembali normal.");
    }

    private void HandleDangerTick(float dt)
    {
        if (!isActive)
        {
            isActive = true;
            remainingSeconds = DANGER_DURATION;
            currentPhase = DOStatus.Danger;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: DO < 5, {DANGER_DURATION}s tersisa.");
        }
        else if (currentPhase == DOStatus.Critical)
        {
            currentPhase = DOStatus.Danger;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: DO naik ke Danger, timer lanjut {remainingSeconds:0}s.");
        }

        TickDown(dt);
    }

    private void HandleCriticalTick(float dt)
    {
        if (!isActive)
        {
            isActive = true;
            remainingSeconds = CRITICAL_DURATION;
            currentPhase = DOStatus.Critical;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: DO < 4, {CRITICAL_DURATION}s tersisa.");
        }
        else if (currentPhase == DOStatus.Danger)
        {
            currentPhase = DOStatus.Critical;
            remainingSeconds = Mathf.Min(remainingSeconds, CRITICAL_DURATION);
            Debug.Log($"[DeathCountdown] {fishInstanceId}: Danger ke Critical, sisa {remainingSeconds:0}s.");
        }

        TickDown(dt);
    }

    private void HandleZeroTick(float dt)
    {
        if (!isActive)
        {
            isActive = true;
            remainingSeconds = ZERO_DURATION;
            currentPhase = DOStatus.Zero;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: DO <= 0, {ZERO_DURATION}s tersisa.");
        }
        else if (currentPhase != DOStatus.Zero)
        {
            currentPhase = DOStatus.Zero;
            remainingSeconds = Mathf.Min(remainingSeconds, ZERO_DURATION);
            Debug.Log($"[DeathCountdown] {fishInstanceId}: DO turun ke 0, sisa {remainingSeconds:0}s.");
        }

        TickDown(dt);
    }

    private void TickDown(float dt)
    {
        remainingSeconds -= dt;
        OnCountdownUpdated?.Invoke(fishInstanceId, remainingSeconds, currentPhase);

        if (remainingSeconds <= 0f)
        {
            isActive = false;
            remainingSeconds = 0f;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: Countdown habis, kematian dipicu.");
            OnDeathTriggered?.Invoke(fishInstanceId);
        }
    }
}
