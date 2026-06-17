using UnityEngine;

public enum ItemType
{
    None,
    BanhMiHuynhHoa,
    PS5Portal,
    CoffeeCup,
    ChauBuiPhone,
    WaterBottle,
    TeacherCard,
    Camera,
}

[CreateAssetMenu(fileName = "ItemData", menuName = "FNAB/Item Data")]
public class ItemData : ScriptableObject
{
    public string   itemName;
    public ItemType itemType;
    [TextArea]
    public string   description;
    public Sprite   icon;
}