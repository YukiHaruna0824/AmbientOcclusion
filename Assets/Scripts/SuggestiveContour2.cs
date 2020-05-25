using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SuggestiveContour2 : MonoBehaviour
{
    private Material scMaterial = null;

    [Range(0.001f, 0.03f)]
    public float Threshold = 0.01f;
    private Camera Cam = null;

    private void Awake()
    {
        var shader = Shader.Find("Unlit/SuggestiveContour2");
        scMaterial = new Material(shader);
        Cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        Cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnDisable()
    {
        Cam.depthTextureMode &= ~DepthTextureMode.Depth;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        scMaterial.SetFloat("_Height", source.height);
        scMaterial.SetFloat("_Width", source.width);
        scMaterial.SetFloat("_Threshold", Threshold);
        Graphics.Blit(source, destination, scMaterial);
    }

}