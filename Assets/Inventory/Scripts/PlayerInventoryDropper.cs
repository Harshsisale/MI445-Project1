
using UnityEngine;

public class PlayerInventoryDropper : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform dropOrigin;
    [SerializeField] private WorldItemPickup fallbackPrefab;
    [SerializeField] private float dropDistance = 1.5f;
    

    public bool TryDrop(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Slots.Count)
            return false;

        ItemDefinition item = inventory.Slots[slotIndex].item;

        GameObject prefab = item.worldPrefab != null
            ? item.worldPrefab
            : fallbackPrefab.gameObject;

        Vector3 direction = dropOrigin.forward;
        direction.y = 0f;
        direction.Normalize();

        Vector3 position =
            dropOrigin.position + direction * dropDistance;

        if (Physics.CheckSphere(position, 0.3f))
            return false;

        GameObject spawned = Instantiate(
            prefab,
            position,
            Quaternion.identity
        );

        WorldItemPickup pickup =
            spawned.GetComponent<WorldItemPickup>();

        if (pickup == null)
        {
            Destroy(spawned);
            return false;
        }

        pickup.Initialize(item, 1);

        if (!inventory.RemoveFromSlot(slotIndex, 1))
        {
            Destroy(spawned);
            return false;
        }

        return true;
    }
}
