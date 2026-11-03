using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class JournalTextController : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TMP_InputField leftPageInput;
    [SerializeField] private TMP_InputField rightPageInput;

    private const string TypingHint = "Click here to type notes...";
    private TMP_Text leftPlaceholder;
    private TMP_Text rightPlaceholder;

    // Notes belong to this journal instance and last for the current game session.
    private string leftPageText = "";
    private string rightPageText = "";
    private bool initialized;

    private void Awake()
    {
        if (leftPageInput == null || rightPageInput == null)
        {
            Debug.LogError("Journal text controller is missing a page input field.", this);
            enabled = false;
            return;
        }

        leftPageInput.onValueChanged.AddListener(SaveLeftText);
        rightPageInput.onValueChanged.AddListener(SaveRightText);
        leftPlaceholder = leftPageInput.placeholder as TMP_Text;
        rightPlaceholder = rightPageInput.placeholder as TMP_Text;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        leftPageInput.SetTextWithoutNotify(leftPageText);
        rightPageInput.SetTextWithoutNotify(rightPageText);
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        UpdateTypingHint(leftPageInput, leftPlaceholder);
        UpdateTypingHint(rightPageInput, rightPlaceholder);
    }

    private static void UpdateTypingHint(TMP_InputField input, TMP_Text placeholder)
    {
        if (placeholder == null || !input.isActiveAndEnabled || !string.IsNullOrEmpty(input.text))
            return;

        // Leave the real input caret unobstructed once the player starts editing.
        string hint = input.isFocused ? "" : TypingHint;
        if (placeholder.text != hint)
            placeholder.text = hint;
    }

    private void SaveLeftText(string value) => leftPageText = value;
    private void SaveRightText(string value) => rightPageText = value;

    private void OnDestroy()
    {
        if (leftPageInput != null)
            leftPageInput.onValueChanged.RemoveListener(SaveLeftText);

        if (rightPageInput != null)
            rightPageInput.onValueChanged.RemoveListener(SaveRightText);
    }
}
