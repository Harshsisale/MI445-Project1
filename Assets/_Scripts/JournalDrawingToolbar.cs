using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class JournalDrawingToolbar : MonoBehaviour
{
    [SerializeField] private JournalDrawingSurface leftSurface;
    [SerializeField] private JournalDrawingSurface rightSurface;
    [SerializeField] private Button pencilButton;
    [SerializeField] private Button eraserButton;

    private bool initialized;

    private void Awake()
    {
        if (leftSurface == null || rightSurface == null || pencilButton == null || eraserButton == null)
        {
            Debug.LogError("Journal drawing toolbar is missing a surface or button reference.", this);
            enabled = false;
            return;
        }

        SelectPencil();
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        pencilButton.onClick.AddListener(SelectPencil);
        eraserButton.onClick.AddListener(SelectEraser);
    }

    private void SelectPencil()
    {
        leftSurface.SetEraser(false);
        rightSurface.SetEraser(false);
    }

    private void SelectEraser()
    {
        leftSurface.SetEraser(true);
        rightSurface.SetEraser(true);
    }

    private void OnDisable()
    {
        if (pencilButton != null)
            pencilButton.onClick.RemoveListener(SelectPencil);

        if (eraserButton != null)
            eraserButton.onClick.RemoveListener(SelectEraser);
    }
}
