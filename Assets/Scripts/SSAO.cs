using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SSAO : MonoBehaviour
{
    private Material ssaoMaterial = null;
    private Camera Cam = null;
    private List<Vector4> SampleVectorList = new List<Vector4>();

    public Texture NoiseMap;
    [Range(0.010f, 1.0f)]
    public float SampleRadius = 0.16f;
    [Range(4, 100)]
    public int SampleCount = 64;
    [Range(0, 2)]
    public int DownSample = 0;

    [Range(1, 4)]
    public int BlurRadius = 2;
    [Range(0, 0.2f)]
    public float BilaterFilterStrength = 0.2f;

    public bool OnlyShowAO = false;

    //shader pass
    public enum SSAOPassName
    {
        GenerateAO = 0,
        BilateralFilter = 1,
        Composite = 2,
        Noise = 3,
    }

    private void Awake()
    {
        var shader = Shader.Find("Unlit/SSAO");
        ssaoMaterial = new Material(shader);
        Cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        Cam.depthTextureMode |= DepthTextureMode.DepthNormals;
    }

    private void OnDisable()
    {
        Cam.depthTextureMode &= ~DepthTextureMode.DepthNormals;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        HemiSphereSample();

        RenderTexture ao = RenderTexture.GetTemporary(source.width >> DownSample, source.height >> DownSample, 0);
        ssaoMaterial.SetTexture("_NoiseTex", NoiseMap);
        ssaoMaterial.SetFloat("Height", Screen.height);
        ssaoMaterial.SetFloat("Width", Screen.width);
        ssaoMaterial.SetMatrix("_InverseProjectionMatrix", Cam.projectionMatrix.inverse);
        ssaoMaterial.SetVectorArray("_SampleVectorArray", SampleVectorList.ToArray());
        ssaoMaterial.SetFloat("_SampleCount", SampleVectorList.Count);
        ssaoMaterial.SetFloat("_SampleRadius", SampleRadius);
        Graphics.Blit(source, ao, ssaoMaterial, (int)SSAOPassName.GenerateAO);

        RenderTexture blur = RenderTexture.GetTemporary(source.width >> DownSample, source.height >> DownSample, 0);
        ssaoMaterial.SetFloat("_BilaterFilterFactor", 1.0f - BilaterFilterStrength);

        ssaoMaterial.SetVector("_BlurRadius", new Vector4(BlurRadius, 0, 0, 0));
        Graphics.Blit(ao, blur, ssaoMaterial, (int)SSAOPassName.BilateralFilter);

        ssaoMaterial.SetVector("_BlurRadius", new Vector4(0, BlurRadius, 0, 0));
        if (OnlyShowAO)
        {
            Graphics.Blit(blur, destination, ssaoMaterial, (int)SSAOPassName.BilateralFilter);
        }
        else
        {
            Graphics.Blit(blur, ao, ssaoMaterial, (int)SSAOPassName.BilateralFilter);
            ssaoMaterial.SetTexture("_AOTex", ao);
            Graphics.Blit(source, destination, ssaoMaterial, (int)SSAOPassName.Composite);
        }

        RenderTexture.ReleaseTemporary(ao);
        RenderTexture.ReleaseTemporary(blur);
    }

    private void HemiSphereSample()
    {
        if (SampleCount == SampleVectorList.Count)
            return;

        SampleVectorList.Clear();
        for (int i = 0; i < SampleCount; i++)
        {
            Vector4 vec = new Vector4(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(0, 1.0f), 1.0f);
            vec.Normalize();
            float scale = (float)i / SampleCount;
            scale = Mathf.Lerp(0.01f, 1.0f, scale * scale);
            vec *= scale;
            SampleVectorList.Add(vec);
        }
    }
}