using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [Tooltip("Color the screen fades from/to.")]
    [SerializeField] private Color fadeColor = Color.black;

    [Tooltip("How long the fade-in takes when the level starts (seconds).")]
    [SerializeField] private float fadeInDuration = 1f;

    [Tooltip("How long the fade-out takes before loading another scene (seconds).")]
    [SerializeField] private float fadeOutDuration = 1f;

    [Tooltip("Seconds to hold on the fade color before the fade-in begins.")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("If true, fades ignore Time.timeScale (still work while paused).")]
    [SerializeField] private bool useUnscaledTime = true;

    private GameObject canvasObj;
    private CanvasGroup group;

    void Awake()
    {
        BuildOverlay();

        // Start fully covered so there's no one-frame flash of the level.
        canvasObj.SetActive(true);
        group.alpha = 1f;
    }

    void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    /// <summary>
    /// Fades the screen out, then loads a scene. Leave sceneName empty to reload the current scene.
    /// </summary>
    public void FadeOutAndLoad(string sceneName)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine(sceneName));
    }

    private IEnumerator FadeInRoutine()
    {
        if (startDelay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(startDelay);
            else yield return new WaitForSeconds(startDelay);
        }

        yield return Fade(group.alpha, 0f, fadeInDuration);

        // Fully clear: turn the overlay off entirely so it can't interfere with anything.
        canvasObj.SetActive(false);
    }

    private IEnumerator FadeOutRoutine(string sceneName)
    {
        canvasObj.SetActive(true);

        yield return Fade(group.alpha, 1f, fadeOutDuration);

        if (string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        group.alpha = to;
    }

    private void BuildOverlay()
    {
        // Canvas that draws on top of everything. No GraphicRaycaster, so it never receives clicks.
        canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        group = canvasObj.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        // Full-screen image stretched to all four corners.
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        Image image = imageObj.AddComponent<Image>();
        image.color = fadeColor;
        image.raycastTarget = false; // clicks pass straight through to UI underneath

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}