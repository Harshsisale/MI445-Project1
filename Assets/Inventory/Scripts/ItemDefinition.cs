
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

    [Header("World Model")]
    public GameObject worldPrefab;

    [Tooltip("Multiplies the prefab's scale when dropped. Use positive X/Y/Z values. (1, 1, 1) keeps the prefab's size.")]
    public Vector3 worldModelScale = Vector3.one;

    private void OnValidate()
    {
        worldModelScale = new Vector3(
            Mathf.Max(0.001f, worldModelScale.x),
            Mathf.Max(0.001f, worldModelScale.y),
            Mathf.Max(0.001f, worldModelScale.z));
    }
}
