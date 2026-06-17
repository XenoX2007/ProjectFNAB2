using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Settings")]
    public int maxItems = 6;

    [Header("UI")]
    public Transform  itemSlotParent;
    public GameObject itemSlotPrefab;
    public GameObject inventoryPanel;

    private List<ItemData> _items = new List<ItemData>();
    private bool           _isOpen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;

        // Teacher card — apply immediately
        if (item.itemType == ItemType.TeacherCard)
        {
            PlayerMovement.Instance?.PickUpCard();
            Debug.Log("[Inventory] Teacher Card collected!");
            return true;
        }

        // Water bottle — consume immediately
        if (item.itemType == ItemType.WaterBottle)
        {
            ThirstSystem.Instance?.Drink(50f);
            Debug.Log("[Inventory] Drank water bottle — thirst +50.");
            return true;
        }

        if (_items.Count >= maxItems)
        {
            Debug.Log("[Inventory] Full!");
            return false;
        }

        _items.Add(item);
        RefreshUI();
        Debug.Log($"[Inventory] Picked up: {item.itemName}");
        return true;
    }

    public bool HasItem(ItemType type)
        => _items.Exists(i => i.itemType == type);

    public bool UseItem(ItemType type)
    {
        var item = _items.Find(i => i.itemType == type);
        if (item == null) return false;
        _items.Remove(item);
        RefreshUI();
        return true;
    }

    public void ToggleInventory()
    {
        _isOpen = !_isOpen;
        inventoryPanel?.SetActive(_isOpen);
    }

    private void RefreshUI()
    {
        if (itemSlotParent == null || itemSlotPrefab == null) return;

        foreach (Transform child in itemSlotParent)
            Destroy(child.gameObject);

        for (int i = 0; i < _items.Count; i++)
        {
            var slot = Instantiate(itemSlotPrefab, itemSlotParent);
            var item = _items[i];

            var img = slot.GetComponentInChildren<Image>();
            if (img && item.icon) img.sprite = item.icon;

            var lbl = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.text = item.itemName;

            int idx = i;
            slot.GetComponent<Button>()?.onClick.AddListener(() => OnItemClicked(idx));
        }
    }

    private void OnItemClicked(int index)
    {
        if (index >= _items.Count) return;
        ItemUseSystem.Instance?.TryUseItem(_items[index]);
    }
}