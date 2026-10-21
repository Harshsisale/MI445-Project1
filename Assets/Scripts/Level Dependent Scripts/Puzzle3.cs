using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

public class Puzzle3 : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask pickupMask = ~0;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private GameObject fallbackPrefab; 

    private float objectsPlacedCounter = 0f;
    
    [SerializeField] List<Transform> PlacedObjectsPosition = new();
    [SerializeField] List<ItemDefinition> NeededObjects = new();
    private List<ItemDefinition> PlacedObjects = new();

    public bool PuzzleSolved = false;
    

    private InputAction interactAction;

    private void Awake()
    {
        interactAction = playerInput.actions.FindAction("Interact");
    }

    private void checkInventory(ref PlayerInventory inventory)
    {
        int index = 0;
        foreach (InventorySlot item in inventory.Slots) 
        {
            foreach (ItemDefinition itDef in NeededObjects)
            {
                if (PlacedObjects.Contains(itDef)) continue;
                if(item.item.itemId == itDef.itemId)
                {
                    GameObject prefab = item.item.worldPrefab != null
                        ? item.item.worldPrefab
                        : fallbackPrefab.gameObject;
                    GameObject spawned = Instantiate(
                        prefab,
                        PlacedObjectsPosition[PlacedObjects.Count].position,
                        PlacedObjectsPosition[PlacedObjects.Count].rotation
                    );
                    Destroy(spawned.GetComponent<WorldItemPickup>());
                    inventory.RemoveFromSlot(index, 1);
                    

                    PlacedObjects.Add(itDef);
                    if (PlacedObjects.Count == PlacedObjectsPosition.Count) PuzzleSolved = true;
                    return;
                }
            }
            index++;
        }
    }
    void Update()
    {
        if (!interactAction.WasPressedThisFrame())
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );
        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            pickupRange,
            pickupMask,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }
        if(hit.collider.gameObject == gameObject)
        {
            checkInventory(ref playerInventory);
        }
    }
}
