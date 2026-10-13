
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int capacity = 12;

    [SerializeField]
    private List<InventorySlot> slots = new();

    public IReadOnlyList<InventorySlot> Slots => slots;
    public int Capacity => capacity;

    public event Action InventoryChanged;
    [ContextMenu("Print Inventory")]
    private void PrintInventory()
    {
        foreach (InventorySlot slot in slots)
            Debug.Log($"{slot.item.displayName}: {slot.quantity}");
    }
    public bool AddItem(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        int remaining = amount;
        int maxStack = Mathf.Max(1, item.maxStackSize);

        // Check that the whole amount fits first.
        long available = 0;

        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item)
                available += maxStack - slot.quantity;
        }

        available += (long)(capacity - slots.Count) * maxStack;

        if (available < amount)
            return false;

        // Fill existing stacks.
        foreach (InventorySlot slot in slots)
        {
            if (slot.item != item)
                continue;

            int space = maxStack - slot.quantity;
            int toAdd = Mathf.Min(space, remaining);

            slot.quantity += toAdd;
            remaining -= toAdd;

            if (remaining == 0)
                break;
        }

        // Create additional stacks when needed.
        while (remaining > 0)
        {
            int toAdd = Mathf.Min(maxStack, remaining);
            slots.Add(new InventorySlot(item, toAdd));
            remaining -= toAdd;
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveFromSlot(int index, int amount = 1)
    {
        if (index < 0 || index >= slots.Count || amount <= 0)
            return false;

        InventorySlot slot = slots[index];

        if (slot.quantity < amount)
            return false;

        slot.quantity -= amount;

        if (slot.quantity == 0)
            slots.RemoveAt(index);

        InventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(ItemDefinition item)
    {
        foreach (InventorySlot slot in slots)
        {
            if (slot.item == item && slot.quantity > 0)
                return true;
        }

        return false;
    }
}

