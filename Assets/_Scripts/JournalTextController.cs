using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class JournalTextController : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TMP_InputField leftPageInput;
    [SerializeField] private TMP_InputField rightPageInput;

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
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized)
            return;

        leftPageInput.SetTextWithoutNotify(leftPageText);
        rightPageInput.SetTextWithoutNotify(rightPageText);
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
