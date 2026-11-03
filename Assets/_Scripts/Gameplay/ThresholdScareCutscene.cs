using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class ThresholdScareCutscene : MonoBehaviour
{
    [SerializeField, Min(0f)] private float delay = 3f;
    [SerializeField, Min(0.01f)] private float turnDuration = 0.75f;
    [SerializeField, Min(0f)] private float minimumRevealTime = 1.25f;
    [SerializeField] private GameObject cloverPrefab;
    [Tooltip("Move this marker in the Scene view to choose where Clover appears. Leave empty to use the player's entry position.")]
    [SerializeField] private Transform cloverSpawnPoint;
    [SerializeField] private bool facePlayer = true;
    [Tooltip("Extra world-space offset from the spawn marker, or the entry position when no marker is assigned.")]
    [SerializeField] private Vector3 cloverOffset;
    [SerializeField] private AudioSource jumpscareSound;
    [Header("Ending")]
    [SerializeField, Min(0f)] private float blackScreenDuration = 3f;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField, Min(0f)] private float menuFadeInDuration = 0.5f;

    private readonly List<Behaviour> suspendedBehaviours = new List<Behaviour>();
    private Coroutine sequence;
    private PlayerMovement controlledPlayer;
    private bool hasTriggered;
    private bool controlsLocked;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private ScreenFader blackoutFader;
    private bool returningToMenu;
    private bool pausedForBlackout;
    private float previousTimeScale;

    public void Play(PlayerMovement player)
    {
        if (!isActiveAndEnabled || hasTriggered || player == null)
            return;

        hasTriggered = true;
        Vector3 entryPosition = player.transform.position;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null && controller.enabled)
            entryPosition.y = controller.bounds.min.y;

        int groundMask = Physics.DefaultRaycastLayers & ~(1 << player.gameObject.layer);
        if (Physics.Raycast(entryPosition + Vector3.up * 0.3f, Vector3.down,
                out RaycastHit ground, 0.8f, groundMask, QueryTriggerInteraction.Ignore))
            entryPosition = ground.point;

        sequence = StartCoroutine(Run(player, entryPosition));
    }

    private IEnumerator Run(PlayerMovement player, Vector3 entryPosition)
    {
        yield return new WaitForSeconds(delay);
        if (player == null || !player.isActiveAndEnabled || player.CutsceneInputLocked)
        {
            sequence = null;
            yield break;
        }

        LockControls(player);
        try
        {
            if (cloverPrefab != null)
            {
                Vector3 spawnPosition = (cloverSpawnPoint != null ? cloverSpawnPoint.position : entryPosition) + cloverOffset;
                Vector3 facing = Vector3.ProjectOnPlane(player.transform.position - spawnPosition, Vector3.up);
                if (facing.sqrMagnitude < 0.001f) facing = player.transform.forward;
                Quaternion rotation = !facePlayer && cloverSpawnPoint != null
                    ? cloverSpawnPoint.rotation : Quaternion.LookRotation(facing);
                Instantiate(cloverPrefab, spawnPosition, rotation);
            }

            Quaternion startRotation = player.transform.rotation;
            float startPitch = player.ViewPitch;
            float elapsed = 0f;
            while (player != null && elapsed < turnDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / turnDuration));
                player.transform.rotation = Quaternion.AngleAxis(180f * progress, Vector3.up) * startRotation;
                player.SetViewPitch(Mathf.Lerp(startPitch, 0f, progress));
                yield return null;
            }

            if (player == null) yield break;
            player.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up) * startRotation;
            player.SetViewPitch(0f);

            float revealTime = minimumRevealTime;
            if (jumpscareSound != null && jumpscareSound.isActiveAndEnabled && jumpscareSound.clip != null)
            {
                jumpscareSound.Play();
                float soundDuration = jumpscareSound.clip.length / Mathf.Max(Mathf.Abs(jumpscareSound.pitch), 0.01f);
                revealTime = Mathf.Max(revealTime, soundDuration);
            }
            yield return new WaitForSeconds(revealTime);

            if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
            {
                Debug.LogError("Cutscene menu scene is not available in Build Settings: " + mainMenuScene, this);
                yield break;
            }

            blackoutFader = FindFirstObjectByType<ScreenFader>();
            if (blackoutFader == null)
                blackoutFader = new GameObject("CutsceneScreenFader").AddComponent<ScreenFader>();
            // Keep hazards from ending the level during the black-screen hold.
            previousTimeScale = Time.timeScale;
            pausedForBlackout = true;
            Time.timeScale = 0f;
            yield return blackoutFader.HoldBlack(blackScreenDuration);

            // Leave gameplay locked until its scene is unloaded. The destination
            // starts covered and fades in without replaying its long intro fade.
            ScreenFader.PrepareSceneFadeIn(mainMenuScene, menuFadeInDuration);
            returningToMenu = true;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(mainMenuScene);
        }
        finally
        {
            RestoreControls();
            sequence = null;
        }
    }

    private void LockControls(PlayerMovement player)
    {
        controlledPlayer = player;
        // Close existing UI before acquiring the lock, so its previous settings
        // are restored before the cutscene temporarily takes control.
        JournalInteractionController journal = player.GetComponent<JournalInteractionController>();
        if (journal != null) journal.CloseJournal();
        InventoryUI[] inventoryUIs = player.GetComponentsInChildren<InventoryUI>(true);
        foreach (InventoryUI inventoryUI in inventoryUIs)
            if (inventoryUI.IsOpen) inventoryUI.Toggle();

        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        player.SetCutsceneInputLocked(true);
        controlsLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Suspend(journal);
        foreach (InventoryUI inventoryUI in inventoryUIs) Suspend(inventoryUI);
        Suspend(player.GetComponent<PlayerItemInteraction>());
        foreach (BodyCamera bodyCamera in player.GetComponentsInChildren<BodyCamera>(true))
            Suspend(bodyCamera);
    }

    private void Suspend(Behaviour component)
    {
        if (component == null || !component.enabled) return;
        suspendedBehaviours.Add(component);
        component.enabled = false;
    }

    private void RestoreControls()
    {
        if (pausedForBlackout)
        {
            if (!returningToMenu) Time.timeScale = previousTimeScale;
            pausedForBlackout = false;
        }
        if (!returningToMenu && blackoutFader != null)
        {
            blackoutFader.ClearBlackout();
            blackoutFader = null;
        }
        if (!controlsLocked) return;
        if (!returningToMenu)
        {
            foreach (Behaviour component in suspendedBehaviours)
                if (component != null) component.enabled = true;
            if (controlledPlayer != null)
                controlledPlayer.SetCutsceneInputLocked(false);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
        suspendedBehaviours.Clear();

        controlledPlayer = null;
        controlsLocked = false;
    }

    private void OnDisable()
    {
        if (sequence != null)
        {
            StopCoroutine(sequence);
            sequence = null;
        }
        RestoreControls();
    }
}
