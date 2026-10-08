using CesiumForUnity;
using Falah.RovSim.Core;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

public class UnderwaterEffect : MonoBehaviour
{
    public float CameraDepth;

    public Color SurfaceColor;
    public Color DepthColor;
    public float AbyssDepth;
    [Inject] public Cesium3DTileset Ces;
    [Inject] public CesiumGeoreference Ref;
    public Transform CameraPos;
    public Camera Cam;
    
    //Ai
    //("Light Attenuation Coefficients (m^-1)")]
    // High red absorption, moderate green, low blue
    public Vector3 extinctionCoefficients = new Vector3(0.35f, 0.08f, 0.03f); 

    [Header("Surface Light Color")]
    public Color surfaceLightColor = new Color(0.9f, 0.95f, 1f);
    public Color surfaceFogColor = new Color(0.1f, 0.4f, 0.5f);

    [Header("Fog Density Settings")]
    public float surfaceFogDensity = 0.01f;
    public float deepFogDensity = 0.08f;
    public float maxFogDepth = 100f;

    [Header("References")]
    public Light mainDirectionalLight;
    [ContextMenu("SampleHeight")]
    async void Start()
    {
        return;
        var latlon = new double3(Ref.latitude, Ref.longitude,0);
        
        var array = new double3[] {Ref.latitude, Ref.longitude,0};
        Debug.Log("Send");
  
        var result = await Ces.SampleHeightMostDetailed(array);

        if(result.warnings.Length > 0)
            Debug.Log(result.warnings[0]);
        foreach (var height in result.longitudeLatitudeHeightPositions)
        {
            Debug.Log(height);
        }
        /*LayerMask mask = LayerMask.GetMask("Cesium");
        if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit, 1000, mask))
        {
            Debug.Log(hit.point);
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        //ManualMethod();
        AiMethod();
    }

    private void ManualMethod()
    {
        CameraDepth = -CameraPos.position.y;
        if (CameraDepth > 0)
        {
            Cam.clearFlags = CameraClearFlags.SolidColor;
            var i = CameraDepth / AbyssDepth;
            var color = Color.Lerp(SurfaceColor, DepthColor, Mathf.Clamp(i, 0, 1.0f));
            Cam.backgroundColor = color;
            RenderSettings.fogColor = color;
        }
        else
        {
            Cam.clearFlags = CameraClearFlags.Skybox;
        }
    }

    private void AiMethod()
    {
        CameraDepth = -CameraPos.position.y;
        if (CameraDepth > 0)
        {
            Cam.clearFlags = CameraClearFlags.SolidColor;
        }
        else
        {
            Cam.clearFlags = CameraClearFlags.Skybox;
        }

        // Calculate depth (positive integer down)
        float depth = Mathf.Max(0f, - transform.position.y);

        // 1. Calculate Beer-Lambert Light Attenuation for RGB
        Color lightAttenuation = new Color(
            Mathf.Exp(-extinctionCoefficients.x * depth),
            Mathf.Exp(-extinctionCoefficients.y * depth),
            Mathf.Exp(-extinctionCoefficients.z * depth),
            1f
        );

        // 2. Apply RGB decay to Directional Light (Sun)
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.color = surfaceLightColor * lightAttenuation;
            // Sun intensity drops off sharply underwater
            mainDirectionalLight.intensity = Mathf.Exp(-0.05f * depth) * 1.5f; 
        }

        // 3. Shift Ambient & Fog Colors
        Color currentFogColor = surfaceFogColor * lightAttenuation;
        RenderSettings.fogColor = currentFogColor;
        RenderSettings.ambientSkyColor = currentFogColor;
        Cam.backgroundColor = currentFogColor;
        
        // 4. Increase Fog Density with Depth (Simulating suspended particles/murkiness)
        float depthFactor = Mathf.Clamp01(depth / maxFogDepth);
        float fogDensity = Mathf.Lerp(surfaceFogDensity, deepFogDensity, depthFactor);
        // Visibility chosen by the instructor (WaterEnvironment) decides the fog once the camera is under water.
        if (RoVPhysics.Water != null && CameraDepth > 0f)
            fogDensity = WaterEnvironment.FogDensityFor(RoVPhysics.Water.VisibilityAt(CameraDepth));
        RenderSettings.fogDensity = fogDensity;
    }
}
