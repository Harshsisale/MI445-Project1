using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class JournalInteractionController : MonoBehaviour
{
    [Header("Journal")]
    [SerializeField] private ItemDefinition journalItem;
    [SerializeField] private GameObject journalUI;

    [Header("Existing Player Systems")]
    [SerializeField] private PlayerItemHolder itemHolder;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private InventoryUI inventoryUI;

    private TMP_InputField[] noteInputs;
    private bool initialized;
    private bool isOpen;
    private bool previousMouseLook;
    private bool previousMovement;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (journalUI != null)
            journalUI.SetActive(false);

        if (journalItem == null || journalUI == null || itemHolder == null || movement == null)
        {
            Debug.LogError("Journal interaction controller is missing an item, UI, holder, or movement reference.", this);
            enabled = false;
            return;
        }

        noteInputs = journalUI.GetComponentsInChildren<TMP_InputField>(true);
        foreach (TMP_InputField input in noteInputs)
        {
            input.onSelect.AddListener(OnNoteSelected);
            input.onDeselect.AddListener(OnNoteDeselected);
        }

        initialized = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (isOpen)
        {
            if (itemHolder.EquippedItem != journalItem || !journalUI.activeSelf ||
                (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            {
                CloseJournal();
                return;
            }

            RefreshTypingMovement();
            return;
        }

        if (keyboard != null && keyboard.jKey.wasPressedThisFrame &&
            Cursor.lockState == CursorLockMode.Locked)
            OpenJournal();
    }

    public void OpenJournal()
    {
        if (!initialized || !isActiveAndEnabled || isOpen ||
            itemHolder.EquippedItem != journalItem || !journalItem.canRead ||
            (inventoryUI != null && inventoryUI.IsOpen))
            return;

        previousMouseLook = movement.MouseLookEnabled;
        previousMovement = movement.MovementEnabled;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        isOpen = true;

        movement.SetMouseLookEnabled(false);
        itemHolder.SetHeldModelVisible(false);
        journalUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshTypingMovement();
    }

    public void CloseJournal()
    {
        if (!isOpen)
            return;

        isOpen = false;
        if (journalUI != null)
            journalUI.SetActive(false);

        if (movement != null)
        {
            movement.SetMouseLookEnabled(previousMouseLook);
            movement.SetMovementEnabled(previousMovement);
        }

        if (itemHolder != null && itemHolder.EquippedItem == journalItem)
            itemHolder.SetHeldModelVisible(true);

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnNoteSelected(string text)
    {
        if (isOpen)
            movement.SetMovementEnabled(false);
    }

    private void OnNoteDeselected(string text)
    {
        if (isOpen)
            RefreshTypingMovement();
    }

    private void RefreshTypingMovement()
    {
        bool typing = false;
        foreach (TMP_InputField input in noteInputs)
        {
            if (input != null && input.isActiveAndEnabled && input.isFocused)
            {
                typing = true;
                break;
            }
        }

        movement.SetMovementEnabled(previousMovement && !typing);
    }

    private void OnDisable()
    {
        CloseJournal();
    }

    private void OnDestroy()
    {
        if (noteInputs == null)
            return;

        foreach (TMP_InputField input in noteInputs)
        {
            if (input == null)
                continue;

            input.onSelect.RemoveListener(OnNoteSelected);
            input.onDeselect.RemoveListener(OnNoteDeselected);
        }
    }
}
