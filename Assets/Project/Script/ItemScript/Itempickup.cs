using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData itemData;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        var playerNode = PlayerMovement.Instance?.CurrentNode;
        if (playerNode == null) return;

        // Find the NavigationNode this item belongs to
        var myNode = GetComponentInParent<NavigationNode>();
        if (myNode == null) return;

        // Only pick up if player is at this node
        if (playerNode == myNode)
        {
            bool picked = InventorySystem.Instance?.AddItem(itemData) ?? false;
            if (picked)
            {
                Debug.Log($"[Pickup] Picked up {itemData?.itemName}");
                Destroy(gameObject);
            }
        }
    }
}