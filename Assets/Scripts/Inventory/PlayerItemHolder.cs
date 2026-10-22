using UnityEngine;

public class PlayerItemHolder : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform holdPoint;

    private GameObject heldModel;

    public ItemDefinition EquippedItem { get; private set; }

    private void OnEnable()
    {
        inventory.InventoryChanged += CheckEquippedItem;
    }

    private void OnDisable()
    {
        inventory.InventoryChanged -= CheckEquippedItem;
        Unequip();
    }

    public bool TryEquip(ItemDefinition item)
    {
        if (item == null || !item.canEquip || item.heldPrefab == null ||
            holdPoint == null || !inventory.HasItem(item))
            return false;

        if (EquippedItem == item)
            return true;

        Unequip();
        heldModel = Instantiate(item.heldPrefab, holdPoint, false);

        foreach (Collider collider in heldModel.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Rigidbody body in heldModel.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        EquippedItem = item;
        return true;
    }

    public void Unequip()
    {
        if (heldModel != null)
        {
            heldModel.SetActive(false);
            Destroy(heldModel);
        }

        heldModel = null;
        EquippedItem = null;
    }

    private void CheckEquippedItem()
    {
        if (EquippedItem != null && !inventory.HasItem(EquippedItem))
            Unequip();
    }
}
