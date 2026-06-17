using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Bag button bottom left → click to open/close 6 item slots
/// Click item → shows detail → click again → uses item
/// 
/// HIERARCHY:
/// Canvas
///   └── InventoryUI (this script)
///         ├── BagButton          ← always visible bottom left
///         │     └── BagIcon      (Image — bag sprite)
///         │
///         ├── BagPanel           ← hidden by default, opens above bag button
///         │     ├── Background   (dark semi-transparent panel)
///         │     ├── SlotGrid     (GridLayoutGroup 2x3)
///         │     │     ├── Slot_0
///         │     │     ├── Slot_1
///         │     │     ├── Slot_2
///         │     │     ├── Slot_3
///         │     │     ├── Slot_4
///         │     │     └── Slot_5
///         │     └── ItemDetailPanel  (shows selected item info)
///         │           ├── ItemIcon
///         │           ├── ItemName
///         │           └── ItemDesc
///         └── CloseArea          ← invisible button covering screen, closes bag on click
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Bag Button — Bottom Left")]
    public Button    bagButton;
    public Image     bagIcon;
    public Sprite    bagOpenSprite;
    public Sprite    bagClosedSprite;

    [Header("Bag Panel — opens above button")]
    public GameObject bagPanel;

    [Header("Slots inside Bag Panel")]
    public Transform  slotGrid;
    public GameObject slotPrefab;
    public int        totalSlots = 6;

    [Header("Item Detail inside Bag Panel")]
    public GameObject      detailPanel;
    public Image           detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDesc;
    public Button          useButton;

    [Header("Colors")]
    public Color normalSlotColor   = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    public Color selectedSlotColor = new Color(0.9f, 0.8f, 0.2f, 0.5f);
    public Color emptySlotColor    = new Color(0.05f, 0.05f, 0.05f, 0.6f);

    // Runtime
    private List<ItemData> _items       = new List<ItemData>();
    private List<SlotUI>   _slots       = new List<SlotUI>();
    private int            _selectedIdx = -1;
    private bool           _isOpen      = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Wire bag button
        bagButton?.onClick.AddListener(ToggleBag);

        // Wire use button
        useButton?.onClick.AddListener(UseSelectedItem);

        // Build slots
        BuildSlots();

        // Start closed
        bagPanel?.SetActive(false);
        detailPanel?.SetActive(false);
    }

    // ── Toggle bag open / close ───────────────────────────────────
    public void ToggleBag()
    {
        _isOpen = !_isOpen;
        bagPanel?.SetActive(_isOpen);

        // Swap bag icon
        if (bagIcon != null)
            bagIcon.sprite = _isOpen ? bagOpenSprite : bagClosedSprite;

        // Reset selection when closing
        if (!_isOpen)
        {
            _selectedIdx = -1;
            detailPanel?.SetActive(false);
        }

        Debug.Log(_isOpen ? "[Bag] Opened" : "[Bag] Closed");
    }

    public void CloseBag()
    {
        if (!_isOpen) return;
        ToggleBag();
    }

    // ── Build slot grid ───────────────────────────────────────────
    private void BuildSlots()
    {
        if (slotGrid == null || slotPrefab == null) return;

        foreach (Transform child in slotGrid)
            Destroy(child.gameObject);
        _slots.Clear();

        for (int i = 0; i < totalSlots; i++)
        {
            var obj  = Instantiate(slotPrefab, slotGrid);
            var slot = obj.GetComponent<SlotUI>() ?? obj.AddComponent<SlotUI>();
            int idx  = i;
            slot.Init(i, () => OnSlotClicked(idx));
            _slots.Add(slot);
        }

        RefreshSlots();
    }

    // ── Slot clicked ──────────────────────────────────────────────
    private void OnSlotClicked(int idx)
    {
        // Clicked empty slot
        if (idx >= _items.Count)
        {
            _selectedIdx = -1;
            detailPanel?.SetActive(false);
            RefreshSlots();
            return;
        }

        // Clicked already selected slot = use item
        if (_selectedIdx == idx)
        {
            UseSelectedItem();
            return;
        }

        // Select slot — show detail
        _selectedIdx = idx;
        ShowDetail(_items[idx]);
        RefreshSlots();
    }

    private void ShowDetail(ItemData item)
    {
        if (detailPanel == null) return;
        detailPanel.SetActive(true);
        if (detailIcon && item.icon) detailIcon.sprite = item.icon;
        if (detailName)             detailName.text   = item.itemName;
        if (detailDesc)             detailDesc.text   = item.description;
    }

    // ── Use selected item ─────────────────────────────────────────
    private void UseSelectedItem()
    {
        if (_selectedIdx < 0 || _selectedIdx >= _items.Count) return;
        var item = _items[_selectedIdx];
        ItemUseSystem.Instance?.TryUseItem(item);
        _selectedIdx = -1;
        detailPanel?.SetActive(false);
        Debug.Log($"[Bag] Used {item.itemName}");
    }

    // ── Refresh all slots ─────────────────────────────────────────
    private void RefreshSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < _items.Count)
                _slots[i].SetItem(_items[i], i == _selectedIdx,
                                  normalSlotColor, selectedSlotColor);
            else
                _slots[i].SetEmpty(emptySlotColor);
        }
    }

    // ── Called by InventorySystem when items change ───────────────
    public void UpdateItems(List<ItemData> items)
    {
        _items       = new List<ItemData>(items);
        _selectedIdx = -1;
        detailPanel?.SetActive(false);
        RefreshSlots();
    }
}

// ════════════════════════════════════════════════════════════════
// SLOT UI
// ════════════════════════════════════════════════════════════════
public class SlotUI : MonoBehaviour
{
    private Image           _bg;
    private Image           _icon;
    private TextMeshProUGUI _label;
    private Button          _btn;

    public void Init(int index, System.Action onClick)
    {
        _bg    = GetComponent<Image>();
        _icon  = transform.Find("ItemIcon")?.GetComponent<Image>();
        _label = transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
        _btn   = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
        _btn.onClick.AddListener(() => onClick?.Invoke());
    }

    public void SetItem(ItemData item, bool selected, Color normal, Color highlight)
    {
        if (_bg)    _bg.color = selected ? highlight : normal;
        if (_icon)  { _icon.gameObject.SetActive(true);  _icon.sprite = item.icon; }
        if (_label) { _label.gameObject.SetActive(true); _label.text  = item.itemName; }
    }

    public void SetEmpty(Color empty)
    {
        if (_bg)    _bg.color = empty;
        if (_icon)  _icon.gameObject.SetActive(false);
        if (_label) _label.gameObject.SetActive(false);
    }
}