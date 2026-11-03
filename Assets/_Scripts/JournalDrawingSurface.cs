using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class JournalDrawingSurface : MonoBehaviour,
    IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler,
    IPointerUpHandler, IPointerExitHandler
{
    [Header("Drawing Settings")]
    [SerializeField, Min(1)] private int textureWidth = 512;
    [SerializeField, Min(1)] private int textureHeight = 768;
    [SerializeField, Min(1)] private int pencilSize = 2;
    [SerializeField, Min(1)] private int eraserSize = 12;
    [SerializeField] private Color pencilColor = new Color(0.15f, 0.12f, 0.1f, 1f);

    [Header("Drawing Boundary")]
    [SerializeField] private Texture2D drawingMask;
    [SerializeField, Range(0f, 1f)] private float maskThreshold = 0.5f;

    private RectTransform rectTransform;
    private Texture2D drawingTexture;
    private Color32[] pixels;
    private bool isDrawing;
    private bool hasPreviousPixel;
    private bool eraserActive;
    private int activePointerId;
    private Vector2Int previousPixel;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        textureWidth = Mathf.Max(1, textureWidth);
        textureHeight = Mathf.Max(1, textureHeight);
        drawingTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            name = gameObject.name + " Drawing",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        // A fresh Color32 array is fully transparent; initialize the GPU copy too.
        pixels = new Color32[textureWidth * textureHeight];
        ApplyTexture();
        RawImage image = GetComponent<RawImage>();
        image.texture = drawingTexture;
        image.raycastTarget = true;

        if (drawingMask != null && !drawingMask.isReadable)
        {
            Debug.LogError("Journal drawing mask must have Read/Write Enabled in its import settings.", this);
            enabled = false;
        }
    }

    public void SetEraser(bool enabled)
    {
        if (eraserActive != enabled)
        {
            isDrawing = false;
            hasPreviousPixel = false;
        }

        eraserActive = enabled;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || eventData.button != PointerEventData.InputButton.Left ||
            (isDrawing && eventData.pointerId != activePointerId) ||
            !TryGetPixel(eventData, out Vector2Int pixel))
            return;

        activePointerId = eventData.pointerId;
        isDrawing = true;
        hasPreviousPixel = true;
        previousPixel = pixel;
        DrawCircle(pixel);
        ApplyTexture();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isActiveAndEnabled || !isDrawing || eventData.pointerId != activePointerId ||
            eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!TryGetPixel(eventData, out Vector2Int pixel))
        {
            hasPreviousPixel = false;
            return;
        }

        // Resume at the entry point instead of connecting across the area
        // traversed while the pointer was outside the page.
        if (hasPreviousPixel)
            DrawLine(previousPixel, pixel);
        else
            DrawCircle(pixel);

        hasPreviousPixel = true;
        previousPixel = pixel;
        ApplyTexture();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId && eventData.button == PointerEventData.InputButton.Left)
        {
            isDrawing = false;
            hasPreviousPixel = false;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
            hasPreviousPixel = false;
    }

    private bool TryGetPixel(PointerEventData eventData, out Vector2Int pixel)
    {
        pixel = default;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return false;

        Rect rect = rectTransform.rect;
        // Check the raw coordinates first: InverseLerp clamps outside points to
        // an edge, which would otherwise let an out-of-page drag keep drawing.
        if (rect.width <= 0 || rect.height <= 0 || local.x < rect.xMin || local.x > rect.xMax ||
            local.y < rect.yMin || local.y > rect.yMax)
            return false;

        float u = (local.x - rect.xMin) / rect.width;
        float v = (local.y - rect.yMin) / rect.height;
        pixel = new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt(u * (textureWidth - 1)), 0, textureWidth - 1),
            Mathf.Clamp(Mathf.RoundToInt(v * (textureHeight - 1)), 0, textureHeight - 1));
        return IsInsideDrawingMask(pixel.x, pixel.y);
    }

    private bool IsInsideDrawingMask(int x, int y)
    {
        if (drawingMask == null)
            return true;

        float u = (x + 0.5f) / textureWidth;
        float v = (y + 0.5f) / textureHeight;
        return drawingMask.GetPixelBilinear(u, v).a >= maskThreshold;
    }

    private void DrawLine(Vector2Int start, Vector2Int end)
    {
        int steps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y));
        if (steps == 0)
        {
            DrawCircle(start);
            return;
        }

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            DrawCircle(new Vector2Int(
                Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t)),
                Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t))));
        }
    }

    private void DrawCircle(Vector2Int center)
    {
        int radius = Mathf.Clamp(eraserActive ? eraserSize : pencilSize, 1, Mathf.Max(textureWidth, textureHeight));
        Color32 color = eraserActive ? new Color32(0, 0, 0, 0) : (Color32)pencilColor;
        int minX = Mathf.Max(0, center.x - radius);
        int maxX = Mathf.Min(textureWidth - 1, center.x + radius);
        int minY = Mathf.Max(0, center.y - radius);
        int maxY = Mathf.Min(textureHeight - 1, center.y + radius);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - center.x;
                int dy = y - center.y;
                if (dx * dx + dy * dy <= radius * radius && IsInsideDrawingMask(x, y))
                    pixels[y * textureWidth + x] = color;
            }
        }
    }

    private void ApplyTexture()
    {
        drawingTexture.SetPixels32(pixels);
        drawingTexture.Apply(false);
    }

    private void OnDisable()
    {
        isDrawing = false;
        hasPreviousPixel = false;
    }

    private void OnDestroy()
    {
        if (drawingTexture != null)
            Destroy(drawingTexture);
    }
}
