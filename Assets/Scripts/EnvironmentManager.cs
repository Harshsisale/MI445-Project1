using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [Header("Outdoor")]
    [SerializeField] private Color outdoorAmbientColor = Color.gray;

    [Header("Indoor")]
    [SerializeField] private Color indoorAmbientColor = Color.black;

// Sets the environment to indoor settings
    public void SetIndoor()
    {
        RenderSettings.fog = false;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = indoorAmbientColor;
    }

// Sets the environment to outdoor settings
    public void SetOutdoor()
    {
        RenderSettings.fog = true;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = outdoorAmbientColor;
    }
}