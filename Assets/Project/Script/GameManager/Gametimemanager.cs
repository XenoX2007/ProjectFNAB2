using UnityEngine;
using System;
using UnityEngine.Events;

/// <summary>
/// Manages the in-game clock (7 PM → 6 AM = 11 real-time hours compressed).
/// Fires events at key story moments:
///   11 PM  → Night begins, NPCs start roaming
///   1 AM   → Chau Bui activates
///   2:30 AM → QuyNguyen arrives with water
///   3 AM   → David Holloway + more NPCs active
///   6 AM   → Deadline check / game over
/// </summary>
public class GameTimeManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────
    public static GameTimeManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────
    [Header("Time Settings")]
    [Tooltip("Total real seconds from 7 PM to 6 AM")]
    public float totalRealSeconds = 660f; // 11 minutes default (tune to taste)

    [Header("Events")]
    public UnityEvent onNightBegins;       // 11 PM
    public UnityEvent onChauBuiActivates;  // 1 AM
    public UnityEvent onQuyNguyenArrives;  // 2:30 AM
    public UnityEvent onDavidActivates;    // 3 AM
    public UnityEvent onPeakDanger;        // 3 AM (more NPCs)
    public UnityEvent onDeadlineTime;      // 6 AM

    [Header("UI")]
    public TMPro.TextMeshProUGUI timeDisplay;

    // ── Runtime ───────────────────────────────────────────────────
    public float  GameHour       { get; private set; }  // 19.0 → 30.0 (30 = 6 AM next day)
    public bool   IsPaused       { get; set; }
    public bool   IsNight        => GameHour >= 23f;
    public string FormattedTime  => FormatTime(GameHour);

    private float _elapsed = 0f;
    private bool  _nightFired, _chauFired, _quyFired, _davidFired, _peakFired, _deadlineFired;

    // ── Constants ─────────────────────────────────────────────────
    private const float START_HOUR    = 19f;  // 7 PM
    private const float END_HOUR      = 30f;  // 6 AM (next day = 24+6)
    private const float TOTAL_HOURS   = END_HOUR - START_HOUR; // 11 hours

    // ── Lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameHour = START_HOUR;
    }

    private void Update()
    {
        if (IsPaused) return;

        _elapsed  += Time.deltaTime;
        GameHour   = START_HOUR + (_elapsed / totalRealSeconds) * TOTAL_HOURS;
        GameHour   = Mathf.Min(GameHour, END_HOUR);

        CheckMilestones();

        if (timeDisplay != null)
            timeDisplay.text = FormattedTime;
    }

    // ── Milestone events ─────────────────────────────────────────

    private void CheckMilestones()
    {
        if (!_nightFired   && GameHour >= 23f)  { _nightFired   = true; onNightBegins?.Invoke(); }
        if (!_chauFired    && GameHour >= 25f)  { _chauFired    = true; onChauBuiActivates?.Invoke(); }
        if (!_quyFired     && GameHour >= 26.5f){ _quyFired     = true; onQuyNguyenArrives?.Invoke(); }
        if (!_davidFired   && GameHour >= 27f)  { _davidFired   = true; onDavidActivates?.Invoke(); }
        if (!_peakFired    && GameHour >= 27f)  { _peakFired    = true; onPeakDanger?.Invoke(); }
        if (!_deadlineFired && GameHour >= 30f) { _deadlineFired= true; onDeadlineTime?.Invoke(); }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private string FormatTime(float hour)
    {
        // Normalise to 0-24 range
        float h = hour % 24f;
        int   hInt  = Mathf.FloorToInt(h);
        int   mInt  = Mathf.FloorToInt((h - hInt) * 60f);
        string amPm = hInt >= 12 ? "AM" : "PM";
        int   h12   = hInt == 0 ? 12 : hInt > 12 ? hInt - 12 : hInt;
        return $"{h12}:{mInt:00} {amPm}";
    }

    /// <summary>Returns 0-1 normalised danger multiplier based on time of night.</summary>
    public float GetDangerMultiplier()
    {
        if (GameHour < 23f)  return 0f;
        if (GameHour < 25f)  return 0.3f;
        if (GameHour < 27f)  return 0.6f;
        return 1.0f;
    }
}