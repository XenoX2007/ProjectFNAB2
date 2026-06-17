using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives all the HUD elements shown in the GDD UI Overview:
/// TIME | THIRST droplets | DEADLINE % | ITEM SLOTS
/// Also handles the camera tablet toggle and peek indicator.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Time")]
    public TextMeshProUGUI timeText;

    [Header("Thirst — 4 droplets")]
    public Image[] thirstDroplets;     // drag 4 droplet images here
    public Color   thirstFull   = new Color(0.3f, 0.6f, 1f);
    public Color   thirstLow    = new Color(0.2f, 0.2f, 0.4f);

    [Header("Deadline")]
    public Slider             deadlineSlider;
    public TextMeshProUGUI    deadlinePercent;

    [Header("Active Item Slot (bottom-left)")]
    public Image              activeItemIcon;
    public TextMeshProUGUI    activeItemName;

    [Header("Peek Indicator")]
    [Tooltip("Small arrow that tilts left/right to show peek direction")]
    public RectTransform peekArrow;
    public float         peekArrowMaxAngle = 25f;

    [Header("Interaction Prompt")]
    public GameObject          interactionPrompt;
    public TextMeshProUGUI     interactionText;

    [Header("Danger Indicator (top-right red dot)")]
    public Image dangerDot;
    public Color dangerOff = Color.white;
    public Color dangerOn  = new Color(1f, 0.1f, 0.1f);

    [Header("Camera / Bag Buttons")]
    public Button cameraButton;
    public Button bagButton;

    // ── Runtime ───────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        cameraButton?.onClick.AddListener(() => CameraSystem.Instance?.ToggleCameras());
        bagButton    ?.onClick.AddListener(() => InventorySystem.Instance?.ToggleInventory());
    }

    private void Update()
    {
        RefreshTime();
        RefreshThirst();
        RefreshDeadline();
        RefreshPeekArrow();
    }

    // ── Refresh methods ───────────────────────────────────────────

    private void RefreshTime()
    {
        if (timeText && GameTimeManager.Instance != null)
            timeText.text = GameTimeManager.Instance.FormattedTime;
    }

    private void RefreshThirst()
    {
        if (ThirstSystem.Instance == null) return;
        float pct = ThirstSystem.Instance.ThirstPercent;

        for (int i = 0; i < thirstDroplets.Length; i++)
        {
            if (thirstDroplets[i] == null) continue;
            float threshold = (float)(i + 1) / thirstDroplets.Length;
            thirstDroplets[i].color = pct >= threshold ? thirstFull : thirstLow;
        }
    }

    private void RefreshDeadline()
    {
        if (DeadlineSystem.Instance == null) return;
        float p = DeadlineSystem.Instance.Progress;

        if (deadlineSlider)  deadlineSlider.value = p / 100f;
        if (deadlinePercent) deadlinePercent.text  = $"{Mathf.FloorToInt(p)}%";
    }

    private void RefreshPeekArrow()
    {
        if (peekArrow == null) return;
        // Read mouse X delta to match PlayerMovement peek
        float peekX = Input.GetAxis("Mouse X");
        float angle = Mathf.Clamp(peekX * peekArrowMaxAngle, -peekArrowMaxAngle, peekArrowMaxAngle);
        peekArrow.localRotation = Quaternion.Euler(0, 0, -angle);
    }

    // ── Public API ────────────────────────────────────────────────

    public void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt) interactionPrompt.SetActive(true);
        if (interactionText)   interactionText.text = text;
    }

    public void HideInteractionPrompt()
    {
        if (interactionPrompt) interactionPrompt.SetActive(false);
    }

    public void SetActiveItem(ItemData item)
    {
        if (activeItemIcon && item?.icon != null) activeItemIcon.sprite = item.icon;
        if (activeItemName)  activeItemName.text = item?.itemName ?? "";
    }

    public void SetDangerState(bool inDanger)
    {
        if (dangerDot) dangerDot.color = inDanger ? dangerOn : dangerOff;
    }
}