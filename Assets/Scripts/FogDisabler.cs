using System;
using UnityEngine;
using UnityEngine.Rendering;

public class FogDisabler : MonoBehaviour
{
    private void Awake()
    {
        RenderPipelineManager.beginCameraRendering += (x,y)=>RenderSettings.fog = false;
        RenderPipelineManager.endCameraRendering += (x,y)=>RenderSettings.fog = true;
    }


}
