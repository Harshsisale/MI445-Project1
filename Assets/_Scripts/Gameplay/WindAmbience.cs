using UnityEngine;

[DefaultExecutionOrder(11000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter))]
public class WindAmbience : MonoBehaviour
{
    [SerializeField] private BodyCamera bodyCamera;
    [SerializeField, Range(0f, 1f)] private float outdoorVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float indoorVolume = 0.15f;
    [SerializeField, Range(10f, 22000f)] private float indoorCutoff = 900f;
    [SerializeField, Min(0.1f)] private float transitionSpeed = 5f;

    private AudioSource wind;
    private AudioLowPassFilter lowPass;

    private void Awake()
    {
        wind = GetComponent<AudioSource>();
        lowPass = GetComponent<AudioLowPassFilter>();
        if (bodyCamera == null)
            bodyCamera = GetComponentInParent<BodyCamera>();

        wind.loop = true;
        wind.playOnAwake = false;
        wind.spatialBlend = 0f;
        wind.dopplerLevel = 0f;
    }

    private void OnEnable()
    {
        UpdateEnvironment(1f);
        if (wind.clip != null)
            wind.Play();
    }

    private void LateUpdate()
    {
        // Ambience continues while standing still or browsing inventory.
        UpdateEnvironment(1f - Mathf.Exp(-transitionSpeed * Time.deltaTime));
    }

    private void UpdateEnvironment(float blend)
    {
        bool indoors = bodyCamera != null && bodyCamera.IsIndoors;
        wind.volume = Mathf.Lerp(wind.volume, indoors ? indoorVolume : outdoorVolume, blend);
        lowPass.cutoffFrequency = Mathf.Lerp(lowPass.cutoffFrequency, indoors ? indoorCutoff : 22000f, blend);
    }

    private void OnDisable()
    {
        wind.Stop();
    }
}
