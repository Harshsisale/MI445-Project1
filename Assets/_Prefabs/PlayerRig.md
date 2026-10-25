# PlayerRig

Drag `PlayerRig.prefab` into a scene and move its root to the player spawn position.
Use one player rig per scene. Replace the old player, inventory Canvas, and
EventSystem when adding this to a scene that already has them. The rig includes
its own Main Camera, so remove any other gameplay camera and AudioListener too.

The prefab contains the working setup from `Teleportation_Test`:

- Player movement, input, inventory, interaction raycast, and item holder.
- `Camera/Main Camera/HoldPoint` for equipped item models.
- `Canvas/InteractionPrompt`, inventory panel, and inventory controls.
- EventSystem for clicking inventory buttons.

All references are assigned inside the prefab. `Puzzle0` and
`Teleportation_Test` use linked instances, with their spawn transforms preserved.

To change all scenes, open `PlayerRig.prefab` in Prefab Mode and edit the
HoldPoint, prompt appearance, interaction range, or other shared settings there.
Keep InteractionPrompt directly under Canvas, outside InventoryPanel.
Scene-specific player placement belongs on the scene instance.

Objects show a prompt when the camera points at a non-trigger Collider within
the interaction range (3 units by default), with an enabled `IInteractable`
component on that collider or one of its parents. Use `WorldItemPickup` for
items, `WorldInteractable` for generic interactions, or the existing door/switch
components. Use the item's `ItemDefinition` asset to choose its held prefab;
edit that held prefab to adjust an individual item's pose.
