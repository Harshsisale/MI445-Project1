using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class GameManager : MonoBehaviour
{
    public float gameTimeMinutes;
    [SerializeField] float  gameTimeSeconds;
    private float totalGameSeconds;
    public GameObject tornado;
    public GameObject tornado2;
    private UniversalRenderPipelineAsset urp;
    [SerializeField, Range(0.1f, 2f)] private float startScale = 1f;
    public float minRenderScale = 0.6f;
    public float startV = 0.8f;
    public float minV = 0.2f;
    public Light envLight;
    public float minLightIntensity = 0.2f;
    public float maxLightIntensity = 0.8f;
    public AudioSource tornadoSFX;
    private bool tornadoChaseRO;
  

    void Start()
    {
        gameTimeSeconds = gameTimeMinutes * 60;
        totalGameSeconds = gameTimeSeconds;
        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        urp.renderScale = startScale;
    }
    void Update()
    {
        gameTimeSeconds -= Time.deltaTime;
        UpdateRenderScale();
        
        
        if (gameTimeSeconds <= 0)
        {
            TornadoChase();
        }
    }

    void TornadoChase()
    {
        if (!tornadoChaseRO)
        {
            tornadoSFX.Play();
            tornado.SetActive(true);
            tornado2.SetActive(true);
            tornadoChaseRO = true;
        }
   
    }
    
    private void UpdateRenderScale()
    {
        if (urp == null || totalGameSeconds <= 0f) return;

        // 0 at the start of the timer, 1 when it hits zero
        float progress = 1f - Mathf.Clamp01(gameTimeSeconds / totalGameSeconds);
        Color.RGBToHSV(RenderSettings.ambientLight, out float h, out float s, out float v);
        v = Mathf.Lerp(startV, minV, progress); // e.g. darken with your timer
        RenderSettings.ambientLight = Color.HSVToRGB(h, s, v, true);
        envLight.intensity = Mathf.Lerp(maxLightIntensity, minLightIntensity, progress);

        urp.renderScale = Mathf.Lerp(startScale, minRenderScale, progress);
    }

}
