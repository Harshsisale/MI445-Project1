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

Footsteps alternate left/right in sync with the camera bob and actual movement:
about 1.9 steps/second walking at 4 m/s and 2.85 sprinting at 6 m/s.
The first step plays as movement starts; opening inventory stops movement and audio.
The rig uses `leftfoot`/`rightfoot (1)` indoors and
`left_step_outside`/`right_step_outside` outdoors. Add `FootstepSurface` to indoor
floor geometry or its parent; unmarked ground uses outdoor sounds. A marker on a
child overrides its parent. Mark indoor stairs too, including stairs outside the
house hierarchy. A wide ground probe keeps footsteps active while descending
treads. The floor is checked each step, including after teleports.
`House/Floor2.prefab` carries an indoor marker directly on its mesh collider;
the attic's floor tiles inherit this setting from the prefab.

Footstep sources use 20% volume. `Main Camera/Wind` loops `wind.wav` with
`WindAmbience`: outdoors it plays at 50% volume with full treble; indoors it fades
to 15% volume with a 900 Hz low-pass filter. The same floor markers select the
environment even when stationary or using inventory. Tune these levels on Wind.

`PlayerInteractionAudio` plays `pickups.wav` when an item is collected or placed
on the puzzle shelf, and `digging.ogg` for each shovel stroke that removes soil.
It also plays `switch.wav` when the player successfully toggles a switch.
These sources use 60% volume and stay on the player so collecting an item
doesn't cut off its sound. Failed interactions stay silent.
Journal page turns play `turn_page.mp3` at 60% volume, synchronized with the page
swap. Both journal layouts use the same dedicated player audio source.

Objects show a prompt when the camera points at a non-trigger Collider within
the interaction range (3 units by default), with an enabled `IInteractable`
component on that collider or one of its parents. Use `WorldItemPickup` for
items, `WorldInteractable` for generic interactions, or the existing door/switch
components. Use the item's `ItemDefinition` asset to choose its held prefab;
edit that held prefab to adjust an individual item's pose.

Pickups have a separate 5 m reach and 0.65 m aiming radius. They can be collected
without lining up an exact ray with a tiny collider, including when very close
or when the pickup collider is a trigger. Player/held-item colliders are ignored.
Solid obstructions still block pickups; buried items must be excavated before
they become reachable. Adjust Pickup Reach and Pickup Aim Radius on the rig's
Player Item Interaction component. Doors, switches, and digging retain their
normal interaction range and direct aiming.
