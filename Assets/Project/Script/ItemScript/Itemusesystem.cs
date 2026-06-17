using UnityEngine;

public class ItemUseSystem : MonoBehaviour
{
    public static ItemUseSystem Instance { get; private set; }

    [Header("NPC References")]
    public NPC_QuocAnh quocAnh;
    public NPC_ChauBui chauBui;
    public NPC_BB      bb;
    public NPC_David   david;

    [Header("Detection Range")]
    public int useRange = 3;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TryUseItem(ItemData item)
    {
        NPCBase target = GetTargetNPC(item.itemType);

        if (target == null)
        {
            Debug.Log($"[ItemUse] {item.itemName} has no NPC target.");
            return;
        }

        int dist = NavigationNode.Distance(
            PlayerMovement.Instance.CurrentNode,
            target.CurrentNode);

        if (dist < 0 || dist > useRange)
        {
            Debug.Log($"[ItemUse] {target.npcName} is too far away.");
            return;
        }

        bool success = target.TryDistract(item.itemType);
        if (success)
            InventorySystem.Instance.UseItem(item.itemType);
    }

    private NPCBase GetTargetNPC(ItemType type)
    {
        return type switch
        {
            ItemType.BanhMiHuynhHoa => (NPCBase)quocAnh,
            ItemType.ChauBuiPhone   => chauBui,
            ItemType.PS5Portal      => bb,
            ItemType.Camera         => bb,
            ItemType.CoffeeCup      => david,
            _                       => null
        };
    }
}