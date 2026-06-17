using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ════════════════════════════════════════════════════════════════
// THIRST SYSTEM
// ════════════════════════════════════════════════════════════════
/// <summary>
/// Tracks the player's thirst (4 water droplet segments like in the GDD UI).
/// Drains over time; reaches 0 → game over.
/// </summary>
public class ThirstSystem : MonoBehaviour
{
    public static ThirstSystem Instance { get; private set; }

    [Header("Settings")]
    [Range(0, 100)] public float maxThirst       = 100f;
    [Range(0, 100)] public float startThirst     = 100f;
    [Tooltip("How many thirst points lost per real second")]
    public float drainRatePerSecond = 0.5f;

    [Header("UI")]
    [Tooltip("Assign 4 Image components for the water droplets")]
    public Image[] dropletImages;
    public Color fullColor  = new Color(0.2f, 0.6f, 1f);
    public Color emptyColor = new Color(0.2f, 0.2f, 0.2f);

    [Header("Warning")]
    public AudioClip thirstWarningClip;
    [Range(0, 100)] public float warningThreshold = 25f;

    private float _thirst;
    private bool  _warningSounded;
    private AudioSource _audio;

    public float ThirstPercent => _thirst / maxThirst;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _audio   = GetComponent<AudioSource>();
        _thirst  = startThirst;
    }

    private void Update()
    {
        // ✓ Safe null check
if (GameManager.Instance == null || !GameManager.Instance.IsGameActive) return;

        _thirst = Mathf.Max(0f, _thirst - drainRatePerSecond * Time.deltaTime);

        UpdateUI();

        if (_thirst <= warningThreshold && !_warningSounded)
        {
            _warningSounded = true;
            _audio?.PlayOneShot(thirstWarningClip);
        }

        if (_thirst <= 0f)
            GameManager.Instance.LoseGame("Dehydrated — thirst reached zero!");
    }

    public void Refill()
    {
        _thirst         = maxThirst;
        _warningSounded = false;
        UpdateUI();
    }

    public void Drink(float amount)
    {
        _thirst = Mathf.Min(maxThirst, _thirst + amount);
        if (_thirst > warningThreshold) _warningSounded = false;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (dropletImages == null) return;
        float fill = ThirstPercent;
        for (int i = 0; i < dropletImages.Length; i++)
        {
            float threshold = (float)(i + 1) / dropletImages.Length;
            dropletImages[i].color = fill >= threshold ? fullColor : emptyColor;
        }
    }
}

// ════════════════════════════════════════════════════════════════
// DEADLINE PROGRESS SYSTEM
// ════════════════════════════════════════════════════════════════
/// <summary>
/// Tracks assignment progress (0 → 100 %).
/// Player must sit at the Main Room PC to fill the bar.
/// Reaching 100% before 6 AM triggers the win condition.
/// </summary>
public class DeadlineSystem : MonoBehaviour
{
    public static DeadlineSystem Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Progress points per second while player is working at PC")]
    public float progressPerSecond = 1.5f;

    [Header("UI")]
    public Slider   progressBar;
    public TextMeshProUGUI percentLabel;

    public float Progress       { get; private set; } = 0f;
    public bool  IsCompleted    => Progress >= 100f;
    public bool  IsWorking      { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!IsWorking || !GameManager.Instance.IsGameActive) return;

        Progress = Mathf.Min(100f, Progress + progressPerSecond * Time.deltaTime);
        UpdateUI();

        if (IsCompleted)
        {
            StopWorking();
            GameManager.Instance.CheckWinCondition();
        }
    }

    /// <summary>Call when player sits down at Main Room PC.</summary>
    public void StartWorking()
    {
        // Player must be at the main room PC node
        var node = PlayerMovement.Instance?.CurrentNode;
        if (node == null || node.nodeType != NodeType.MainRoom)
        {
            Debug.Log("[Deadline] Must be in the Main Room to work on the deadline!");
            return;
        }
        IsWorking = true;
        Debug.Log("[Deadline] Started working on deadline...");
    }

    /// <summary>Call when player leaves the PC or gets disturbed.</summary>
    public void StopWorking()
    {
        IsWorking = false;
    }

    public void Interrupt(float penaltyPercent = 5f)
    {
        // BB or a disturbance interrupts work
        StopWorking();
        Progress = Mathf.Max(0f, Progress - penaltyPercent);
        UpdateUI();
        Debug.Log($"[Deadline] Work interrupted! Progress penalty: -{penaltyPercent}%");
    }

    private void UpdateUI()
    {
        if (progressBar)  progressBar.value       = Progress / 100f;
        if (percentLabel) percentLabel.text        = $"{Mathf.FloorToInt(Progress)}%";
    }
}

// ════════════════════════════════════════════════════════════════
// GAME MANAGER — Win / Lose
// ════════════════════════════════════════════════════════════════
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;
    public TextMeshProUGUI loseReasonText;

    [Header("Scene")]
    public string mainMenuScene = "MainMenu";

    public bool IsGameActive { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Lose if deadline not done by 6 AM
        GameTimeManager.Instance.onDeadlineTime.AddListener(OnDeadlineTimeReached);
        // Subscribe to player caught event
        GameEvents.OnPlayerCaught += reason => LoseGame($"Caught by {reason}!");
    }

    private void OnDeadlineTimeReached()
    {
        if (!DeadlineSystem.Instance.IsCompleted)
            LoseGame("6 AM — deadline not completed in time!");
    }

    public void CheckWinCondition()
    {
        if (DeadlineSystem.Instance.IsCompleted && GameTimeManager.Instance.GameHour < 30f)
            WinGame();
    }

    public void WinGame()
    {
        IsGameActive = false;
        winPanel?.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log("[GameManager] WIN!");
    }

    public void LoseGame(string reason)
    {
        if (!IsGameActive) return;
        IsGameActive = false;
        if (loseReasonText) loseReasonText.text = reason;
        losePanel?.SetActive(true);
        Time.timeScale = 0f;
        Debug.Log($"[GameManager] LOSE — {reason}");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
    }
}