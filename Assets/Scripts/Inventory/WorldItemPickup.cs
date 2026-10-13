
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int quantity = 1;

    private bool claimed;

    public string ItemName =>
        item != null ? item.displayName : "Unknown Item";

    public void Initialize(ItemDefinition newItem, int newQuantity)
    {
        item = newItem;
        quantity = Mathf.Max(1, newQuantity);
        claimed = false;
    }
    public void Interact(PlayerInventory inventory)
    {
        TryPickup(inventory);
    }
    public bool TryPickup(PlayerInventory inventory)
    {
        if (claimed || inventory == null || item == null)
            return false;

        claimed = true;

        if (!inventory.AddItem(item, quantity))
        {
            claimed = false;
            return false;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
