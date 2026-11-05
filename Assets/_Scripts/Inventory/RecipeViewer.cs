using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RecipeViewer : MonoBehaviour
{
    private PlayerMovement movement;
    private WorldRecipeInteractable source;
    private GameObject overlay;
    private Image recipeImage;
    private bool previousMovement;
    private bool previousMouseLook;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private int openedFrame;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
    }

    public void Open(WorldRecipeInteractable recipe, Sprite sprite)
    {
        if (!isActiveAndEnabled || IsOpen || recipe == null || sprite == null ||
            movement == null || movement.CutsceneInputLocked ||
            Cursor.lockState != CursorLockMode.Locked)
            return;

        if (overlay == null)
            CreateOverlay();

        source = recipe;
        recipeImage.sprite = sprite;
        previousMovement = movement.MovementEnabled;
        previousMouseLook = movement.MouseLookEnabled;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        openedFrame = Time.frameCount;
        IsOpen = true;

        movement.SetMovementEnabled(false);
        movement.SetMouseLookEnabled(false);
        overlay.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Start checking dismissal on the frame after opening.
        if (!IsOpen || Time.frameCount == openedFrame)
            return;

        Keyboard keyboard = Keyboard.current;
        if (source == null || !source.isActiveAndEnabled || movement == null ||
            movement.CutsceneInputLocked ||
            (keyboard != null && keyboard.escapeKey.wasPressedThisFrame))
            Close();
    }

    public void Close(WorldRecipeInteractable recipe)
    {
        if (source == recipe)
            Close();
    }

    private void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        source = null;
        if (overlay != null)
            overlay.SetActive(false);

        if (movement != null)
        {
            movement.SetMovementEnabled(previousMovement);
            movement.SetMouseLookEnabled(previousMouseLook);
        }

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void CreateOverlay()
    {
        overlay = new GameObject("Recipe Viewer", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(Image));
        overlay.SetActive(false);
        overlay.transform.SetParent(transform, false);

        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);
        backdrop.raycastTarget = false;

        GameObject imageObject = new GameObject("Recipe Image", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(overlay.transform, false);
        recipeImage = imageObject.GetComponent<Image>();
        recipeImage.preserveAspect = true;
        recipeImage.raycastTarget = false;
        SetRect(recipeImage.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.96f));

    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDisable()
    {
        Close();
    }

    private void OnDestroy()
    {
        Close();
        if (overlay != null)
            Destroy(overlay);
    }
}
