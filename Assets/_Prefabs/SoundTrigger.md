# SoundTrigger

Drag `SoundTrigger.prefab` into the scene at a doorway or crossing. The root sits
at floor height; its invisible trigger is 2 m wide, 3 m tall, and 0.3 m deep.
Resize the Box Collider to cover the threshold. Its cyan outline appears when selected.

The first Audio Source plays `Sounds/erie_sound.wav` at 70% volume on entry.
Assign a different Audio Resource or Volume as needed. Both sources use 2D
playback; change Spatial Blend to locate the sound at the threshold instead.

Players are identified by a `PlayerMovement` component on their collider or a parent.
Other objects do not activate it. Multiple colliders won't duplicate the sequence.
The prefab starts with Play Once enabled.

To place Clover, select the trigger and click **Select Clover Spawn Point** on
`ThresholdScareCutscene`, then move the `CloverSpawnPoint` child with the Scene
view Move tool. A magenta spawn guide is also shown while selecting the trigger.
You can move the marker with that handle without moving the threshold itself.
By default Clover faces the player. Turn off Face Player to use the marker's
rotation instead. Clear Clover Spawn Point to use the recorded player entry
position; Clover Offset adds an optional world-space adjustment in either mode.

After 3 seconds, it closes inventory/journal UI, locks movement and mouse look,
and spawns `3DModels/Clover.prefab` at the selected spawn position.
It turns the player/view 180 degrees over 0.75 seconds and levels the camera.
Once the turn finishes, the second Audio Source plays `Sounds/jumpscare.mp3`.
After that clip finishes (at least 1.25 seconds of reveal time), the screen cuts
fully to black for 3 real-time seconds, then loads `MainMenu` with a 0.5-second
fade-in. Gameplay controls remain locked until the scene changes; the menu
receives an unlocked, visible cursor. Gameplay pauses during the black hold so
hazards cannot interrupt it. Leaving the trigger during the delay does
not cancel the scare; disabling/destroying it cancels the sequence, clears any
blackout, and restores controls.

Delay, Turn Duration, Minimum Reveal Time, Clover Prefab, and the jumpscare Audio
Source are configurable on the cutscene component. Disable that
component for a sound-only trigger; turn off Play Once to allow repeated entry
sounds after the cooldown. The cutscene itself runs once per trigger instance.
All prefab references are assigned; no extra player components are needed.
Black Screen Duration, Main Menu Scene, and Menu Fade In Duration configure the
ending. `MainMenu` is already enabled in the project's Build Settings.
