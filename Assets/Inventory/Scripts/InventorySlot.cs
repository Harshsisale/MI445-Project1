
using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemDefinition item;
    public int quantity;

    public InventorySlot(ItemDefinition item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}
