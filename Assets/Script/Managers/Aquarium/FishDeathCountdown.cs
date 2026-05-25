using System;
using UnityEngine;

/// <summary>
/// Mengelola countdown kematian ikan akibat DO rendah.
/// Menangani transisi Danger (240 detik) → Critical (60 detik) tanpa timer overlap.
///
/// Aturan:
///   DO < 5  → Danger  : countdown mulai dari 240 detik.
///   DO < 4  → Critical: countdown dipercepat — sisa waktu dibatasi menjadi 60 detik.
///   DO ≥ 5  → Safe    : countdown dibatalkan.
/// </summary>
public class FishDeathCountdown
{
    private const float DANGER_DURATION  = 240f;
    private const float CRITICAL_DURATION = 60f;

    private readonly string fishInstanceId;

    private bool    isActive;
    private float   remainingSeconds;
    private DOStatus currentPhase;

    /// <summary>Event dipanggil saat ikan mati karena countdown habis.</summary>
    public event Action<string> OnDeathTriggered;

    /// <summary>Event dipanggil setiap kali sisa waktu berubah (opsional, untuk UI).</summary>
    public event Action<string, float, DOStatus> OnCountdownUpdated;

    public bool    IsActive          => isActive;
    public float   RemainingSeconds  => remainingSeconds;
    public DOStatus CurrentPhase     => currentPhase;

    public FishDeathCountdown(string instanceId)
    {
        fishInstanceId = instanceId;
    }

    // ─── API Publik ──────────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil tiap frame dengan DO status terkini dan Time.deltaTime.
    /// Mengelola seluruh transisi state secara otomatis.
    /// </summary>
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
        }
    }

    /// <summary>
    /// Paksa batalkan countdown (misalnya ikan dipindah atau mati karena sebab lain).
    /// </summary>
    public void Cancel()
    {
        if (!isActive) return;

        isActive = false;
        remainingSeconds = 0f;
        Debug.Log($"[DeathCountdown] {fishInstanceId}: Countdown dibatalkan — DO kembali normal.");
    }

    // ─── Handler State ───────────────────────────────────────────────────────

    private void HandleDangerTick(float dt)
    {
        if (!isActive)
        {
            // Mulai countdown baru dari Danger
            isActive = true;
            remainingSeconds = DANGER_DURATION;
            currentPhase = DOStatus.Danger;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: Danger dimulai — {DANGER_DURATION}s tersisa.");
        }
        else if (currentPhase == DOStatus.Critical)
        {
            // DO naik dari Critical ke Danger → biarkan timer lanjut, jangan reset
            // Tapi set ulang batas atas agar tidak melebihi DANGER_DURATION
            currentPhase = DOStatus.Danger;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: DO naik ke Danger — timer lanjut {remainingSeconds:0}s.");
        }

        TickDown(dt);
    }

    private void HandleCriticalTick(float dt)
    {
        if (!isActive)
        {
            // Langsung masuk Critical tanpa melalui Danger terlebih dahulu
            isActive = true;
            remainingSeconds = CRITICAL_DURATION;
            currentPhase = DOStatus.Critical;
            Debug.Log($"[DeathCountdown] {fishInstanceId}: Critical langsung dimulai — {CRITICAL_DURATION}s tersisa.");
        }
        else if (currentPhase == DOStatus.Danger)
        {
            // Transisi dari Danger ke Critical: batasi sisa waktu ke maximum Critical
            currentPhase = DOStatus.Critical;
            remainingSeconds = Mathf.Min(remainingSeconds, CRITICAL_DURATION);
            Debug.Log($"[DeathCountdown] {fishInstanceId}: Transisi Danger→Critical — sisa {remainingSeconds:0}s.");
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
            Debug.Log($"[DeathCountdown] {fishInstanceId}: Countdown habis — kematian dipicu!");
            OnDeathTriggered?.Invoke(fishInstanceId);
        }
    }
}
