
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement movement;

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotGrid;
    [SerializeField] private Button slotTemplate;

    [SerializeField] private GameObject contextMenu;
    [SerializeField] private Button useButton;
    [SerializeField] private Button examineButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private TMP_Text descriptionText;

    private readonly List<Button> slotButtons = new();
    private InputAction toggleAction;
    private int selectedIndex = -1;
    private bool isOpen;

    private void Awake()
    {
        toggleAction =
            playerInput.actions.FindAction("ToggleInventory");

        inventoryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        inventory.InventoryChanged += Refresh;

        useButton.onClick.AddListener(UseSelected);
        examineButton.onClick.AddListener(ExamineSelected);
        dropButton.onClick.AddListener(DropSelected);
    }

    private void OnDisable()
    {
        inventory.InventoryChanged -= Refresh;

        useButton.onClick.RemoveListener(UseSelected);
        examineButton.onClick.RemoveListener(ExamineSelected);
        dropButton.onClick.RemoveListener(DropSelected);
    }

    private void Update()
    {
        if (toggleAction.WasPressedThisFrame())
            Toggle();
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);

        Cursor.lockState = isOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = isOpen;

        if (movement != null)
        {
            movement.enabled = !isOpen;
        }

        if (isOpen)
        {
            selectedIndex = -1;
            contextMenu.SetActive(false);
            descriptionText.text = "";
            Refresh();
        }
    }

    private void Refresh()
    {
        if (!isOpen)
            return;

        while (slotButtons.Count < inventory.Capacity)
        {
            Button button =
                Instantiate(slotTemplate, slotGrid);

            button.gameObject.SetActive(true);

            int index = slotButtons.Count;
            button.onClick.AddListener(() => SelectSlot(index));

            slotButtons.Add(button);
        }

        for (int i = 0; i < slotButtons.Count; i++)
        {
            Button button = slotButtons[i];
            bool occupied = i < inventory.Slots.Count;

            button.interactable = occupied;

            TMP_Text label =
                button.GetComponentInChildren<TMP_Text>();

            Image icon = button.transform.Find("Icon")
                .GetComponent<Image>();

            if (occupied)
            {
                InventorySlot slot = inventory.Slots[i];

                label.text = slot.item.displayName +
                    " x" + slot.quantity;

                icon.sprite = slot.item.icon;
                icon.enabled = slot.item.icon != null;
            }
            else
            {
                label.text = "";
                icon.sprite = null;
                icon.enabled = false;
            }
        }

        if (selectedIndex >= inventory.Slots.Count)
        {
            selectedIndex = -1;
            contextMenu.SetActive(false);
        }
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= inventory.Slots.Count)
            return;

        selectedIndex = index;
        contextMenu.SetActive(true);
        descriptionText.text = "";
    }

    private void UseSelected()
    {
        if (selectedIndex < 0)
            return;

        descriptionText.text =
            "Nothing nearby to use this item on.";
    }

    private void ExamineSelected()
    {
        if (selectedIndex < 0)
            return;

        descriptionText.text =
            inventory.Slots[selectedIndex].item.description;
    }

    private void DropSelected()
    {
        if (selectedIndex < 0)
            return;

        descriptionText.text =
            "Dropping will be implemented next.";
    }
}
