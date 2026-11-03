using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class JournalPageController : MonoBehaviour
{
    [Header("Journal References")]
    [SerializeField] private RectTransform bookContainer;
    [SerializeField] private Image bookImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Page Sprites")]
    [SerializeField] private Sprite[] pageSpreads;

    [Header("Page Content")]
    [SerializeField] private GameObject linedTextContent;
    [SerializeField, Min(0)] private int linedSpreadIndex = 2;

    [Header("Page Transition")]
    [SerializeField, Min(0f)] private float lowerDistance = 900f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.2f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pageTurnSound;

    private int currentPage;
    private bool isTurning;
    private bool initialized;
    private Vector2 originalPosition;

    private void Awake()
    {
        if (bookContainer == null || bookImage == null ||
            previousButton == null || nextButton == null)
        {
            Debug.LogError("Journal page controller is missing a UI reference.", this);
            enabled = false;
            return;
        }

        if (pageSpreads == null || pageSpreads.Length == 0)
        {
            Debug.LogError("Journal has no page sprites assigned.", this);
            enabled = false;
            return;
        }

        foreach (Sprite spread in pageSpreads)
        {
            if (spread == null)
            {
                Debug.LogError("Journal has an empty page sprite slot.", this);
                enabled = false;
                return;
            }
        }

        originalPosition = bookContainer.anchoredPosition;
        currentPage = 0;
        bookImage.sprite = pageSpreads[currentPage];
        initialized = true;
        UpdatePageContent();
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        previousButton.onClick.AddListener(PreviousPage);
        nextButton.onClick.AddListener(NextPage);
        UpdateButtons();
        UpdatePageContent();
    }

    private void OnDisable()
    {
        ClearTextFocus();

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(PreviousPage);
            previousButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
            nextButton.interactable = false;
        }

        // Disabling the component alone does not stop its coroutines.
        StopAllCoroutines();
        isTurning = false;

        if (initialized && bookContainer != null)
            bookContainer.anchoredPosition = originalPosition;
    }

    private void PreviousPage() => TurnPage(-1);
    private void NextPage() => TurnPage(1);

    public void TurnPage(int direction)
    {
        if (!initialized || !isActiveAndEnabled || isTurning ||
            (direction != -1 && direction != 1))
            return;

        int targetPage = currentPage + direction;
        if (targetPage < 0 || targetPage >= pageSpreads.Length)
            return;

        StartCoroutine(PageTurnRoutine(targetPage));
    }

    private IEnumerator PageTurnRoutine(int targetPage)
    {
        isTurning = true;
        ClearTextFocus();
        UpdateButtons();

        Vector2 loweredPosition = originalPosition + Vector2.down * lowerDistance;
        yield return MoveBook(bookContainer.anchoredPosition, loweredPosition);

        currentPage = targetPage;
        bookImage.sprite = pageSpreads[currentPage];
        UpdatePageContent();

        if (audioSource != null && pageTurnSound != null)
            audioSource.PlayOneShot(pageTurnSound);

        yield return MoveBook(loweredPosition, originalPosition);

        isTurning = false;
        UpdateButtons();
    }

    private IEnumerator MoveBook(Vector2 start, Vector2 end)
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            // Keep UI transitions working when gameplay time is paused.
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = t * t * (3f - 2f * t);
            bookContainer.anchoredPosition = Vector2.LerpUnclamped(start, end, t);
            yield return null;
        }

        bookContainer.anchoredPosition = end;
    }

    private void UpdatePageContent()
    {
        if (linedTextContent == null)
            return;

        bool showText = currentPage == linedSpreadIndex;
        if (!showText)
            ClearTextFocus();

        linedTextContent.SetActive(showText);
    }

    private void ClearTextFocus()
    {
        EventSystem eventSystem = EventSystem.current;
        if (linedTextContent == null || eventSystem == null)
            return;

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(linedTextContent.transform))
            eventSystem.SetSelectedGameObject(null);
    }

    private void UpdateButtons()
    {
        previousButton.interactable = !isTurning && currentPage > 0;
        nextButton.interactable = !isTurning && currentPage < pageSpreads.Length - 1;
    }
}
