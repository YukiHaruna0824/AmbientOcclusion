using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SuggestiveContour : MonoBehaviour
{
    private Material scMaterial = null;

    [Range(0.01f, 0.1f)]
    public float ThresholdScalar = 0.01f;
    [Range(1, 15)]
    public int DetectRadius = 4;

    private void Awake()
    {
        var shader = Shader.Find("Unlit/SuggestiveContour");
        scMaterial = new Material(shader);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        scMaterial.SetFloat("_Height", source.height);
        scMaterial.SetFloat("_Width", source.width);
        scMaterial.SetFloat("_DetectRadius", DetectRadius);
        scMaterial.SetFloat("_ThresholdScalar", ThresholdScalar);
        Graphics.Blit(source, destination, scMaterial);
    }

}