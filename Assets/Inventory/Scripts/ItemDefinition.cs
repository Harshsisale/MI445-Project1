
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Inventory/Item Definition"
)]
public class ItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    [TextArea] public string description;

    [Min(1)] public int maxStackSize = 1;

    public Sprite icon;
    public GameObject worldPrefab;
}
