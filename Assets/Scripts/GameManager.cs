using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class GameManager : MonoBehaviour
{
    public Camera Cam;
    public UIManager UiManager;

    // ambient occulusion sample
    public int AoSample = 64;
    public float SampleRadius = 0.2f;

    // hack model
    public GameObject Cone;
    public GameObject Model;

    // render result
    private Texture2D _renderResult;
    private bool _displayTexture = false;

    // hide ui panel
    private bool _hidePanel = true;

    // relation of gameobject and raytracingobject
    private Dictionary<GameObject, RayTracingObject> _lightObjects = new Dictionary<GameObject, RayTracingObject>();
    private Dictionary<GameObject, RayTracingObject> _objects = new Dictionary<GameObject, RayTracingObject>();

    #region Scene Fucntion 
    private Texture2D LoadImage(string filepath)
    {
        Texture2D tex = null;
        byte[] fileData;
        if (File.Exists(filepath))
        {
            fileData = File.ReadAllBytes(filepath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
        }
        else
            tex = Texture2D.blackTexture;
        return tex;
    }
    private void CreateLight(RayTracingObject rObj)
    {
        GameObject lightObj = new GameObject();
        lightObj.transform.position = rObj.pos;

        Light lightComp = lightObj.AddComponent<Light>();
        lightComp.color = new Color(rObj.lightColor.x, rObj.lightColor.y, rObj.lightColor.z);

        switch (rObj.l_type)
        {
            case LightType.point:
                lightComp.type = UnityEngine.LightType.Point;
                lightObj.name = "PointLight";
                _lightObjects[lightObj] = rObj;
                break;
            case LightType.area:
                lightComp.type = UnityEngine.LightType.Area;
                lightComp.name = "AreaLight";
                lightComp.areaSize = new Vector2(rObj.whrInfo.x, rObj.whrInfo.y);
                _lightObjects[lightObj] = rObj;
                break;
            default:
                print("Create Line Object Failed !");
                break;
        }
    }
    private void CreateObject(RayTracingObject rObj)
    {
        GameObject obj = null;
        float mult = 1.0f;
        switch (rObj.o_type)
        {
            case ObjectType.sphere:
                obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.transform.position = rObj.pos;
                obj.transform.eulerAngles = rObj.rot;
                // default radius 0.5
                mult = rObj.whrInfo.z / 0.5f;
                obj.transform.localScale = rObj.scale * mult;
                break;
            case ObjectType.cylinder:
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.transform.position = rObj.pos;
                obj.transform.eulerAngles = rObj.rot;
                
                // delete default collider
                Destroy(obj.GetComponent<SphereCollider>());
                obj.AddComponent<MeshCollider>();

                // default yminmax +-1 radius 0.5
                mult = rObj.whrInfo.z / 0.5f;
                obj.transform.localScale = new Vector3(rObj.scale.x * mult, rObj.scale.y * rObj.yminmax.y, rObj.scale.z * mult);
                break;
            case ObjectType.cone:
                //default radius 1 height 2
                obj = Instantiate(Cone, rObj.pos, Quaternion.Euler(rObj.rot));
                obj.transform.localScale = new Vector3(rObj.scale.x * rObj.whrInfo.z, rObj.scale.y * rObj.whrInfo.y / 2.0f, rObj.scale.z * rObj.whrInfo.z);
                
                //Special Get Child
                obj = obj.transform.GetChild(0).gameObject;
                break;
            case ObjectType.plane:
                // default size 10 * 10
                obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                obj.transform.position = rObj.pos;
                obj.transform.eulerAngles = rObj.rot;
                obj.transform.localScale = new Vector3(rObj.scale.x * rObj.whrInfo.x / 10f, rObj.scale.y, rObj.scale.z * rObj.whrInfo.y / 10f);
                break;
            case ObjectType.mesh:
                //string rootFolder = Directory.GetParent(mattr.meshName).FullName;
                //loader.Load(rootFolder, Path.GetFileName(mattr.meshName));

                //blender object
                obj = Instantiate(Model, rObj.pos, Quaternion.Euler(new Vector3(rObj.rot.x, 180, rObj.rot.z)));
                obj.transform.localScale = rObj.scale;

                //Special Get Child
                obj = obj.transform.GetChild(0).gameObject;
                break;
            default:
                print("Create Object Failed !");
                return;
        }

        if(rObj.m_type == MaterialType.map)
        {
            Texture2D c_map = LoadImage(rObj.mattr.cmapName);
            Texture2D b_map = LoadImage(rObj.mattr.bmapName);
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetTexture("_MainTex", c_map);
            mat.SetTexture("_BumpMap", b_map);
            obj.GetComponent<MeshRenderer>().material = mat;
        }

        //bind raytracinginfo to gameobject
        _objects[obj] = rObj;

    }
    public void GenerateScene(RayTracingInfo rtInfo, OutputInfo outputInfo)
    {
        // set screen resolution (not working in editor)
        Screen.SetResolution((int)outputInfo.Resolution.x, (int)outputInfo.Resolution.y, false);

        // set camera info
        Cam.transform.position = rtInfo.cameraPos;
        Cam.transform.LookAt(rtInfo.cameraCenter, rtInfo.cameraUp);
        
        // default perspective
        if (rtInfo.cameraType == 1)
            Cam.orthographic = true;

        // create scene objects
        foreach(RayTracingObject rObj in rtInfo.objects)
        {
            // light type
            if (rObj.o_type == ObjectType.none)
                CreateLight(rObj);

            //object type
            if (rObj.l_type == LightType.none)
                CreateObject(rObj);
        }
    }
    public void ClearScene()
    {
        foreach (GameObject obj in _lightObjects.Keys)
            Destroy(obj);
        foreach (GameObject obj in _objects.Keys)
            Destroy(obj);

        _lightObjects.Clear();
        _objects.Clear();
    }
    #endregion


    private Vector3 HemiSphereSample(Vector3 normal, float spread)
    {
        Vector3 b3 = normal.normalized;
        Vector3 different = (Mathf.Abs(b3.x) < 0.5f) ? new Vector3(1.0f, 0.0f, 0.0f) : new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 b1 = Vector3.Cross(b3, different).normalized;
        Vector3 b2 = Vector3.Cross(b1, b3);

        // Pick (x,y,z) randomly around (0,0,1)
        float z = Random.Range(Mathf.Cos(spread * Mathf.PI), 1);

        float r = Mathf.Sqrt(1 - z * z);
        float theta = Random.Range(-Mathf.PI, Mathf.PI);
        float x = r * Mathf.Cos(theta);
        float y = r * Mathf.Sin(theta);

        // Construct the vector that has coordinates (x,y,z) in the basis formed by b1, b2, b3
        return x * b1 + y * b2 + z * b3;
    }
    private Color AoRayTrace(Ray ray)
    {
        float aoValue = 1f;
        if (Physics.Raycast(ray, out RaycastHit hit, 200))
        {
            Vector3 hitPoint = hit.point;
            Vector3 hitNormal = hit.normal;

            // Compute AO value
            int occluded = 0;
            for (int i = 0; i < AoSample; i++)
            {
                Vector3 sample = HemiSphereSample(hitNormal, 0.5f);
                Ray aoRay = new Ray(hitPoint + 0.0001f * hitNormal, sample);
                if (Physics.Raycast(aoRay, out RaycastHit aohit, SampleRadius))
                    occluded++;
            }

            aoValue = 1 - ((float)occluded / AoSample);
        }

        return new Color(aoValue, aoValue, aoValue);
    }
    public void StartRender(RayTracingInfo rtInfo, OutputInfo outputInfo)
    {
        int sample = (int)Mathf.Sqrt(rtInfo.sampleCount);
        Vector2 resolution = outputInfo.Resolution;

        Vector2 ratio = new Vector2(Screen.width / resolution.x, Screen.height / resolution.y);
        Vector2 s_ratio = ratio / sample;

        _renderResult = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGB24, false);

        Vector2 rayPoint = Vector2.zero;
        Color pixelColor = new Color(0, 0, 0);
        for(int w = 0; w < resolution.x; w++)
        {
            for(int h = 0; h < resolution.y; h++)
            {
                rayPoint = new Vector2(w * ratio.x, h * ratio.y);
                Color pixel = Color.black;
                for(int sw = 0; sw < sample; sw++)
                {
                    for(int sh = 0; sh < sample; sh++)
                    {
                        rayPoint += new Vector2(sw * s_ratio.x, sh * s_ratio.y);
                        Ray ray = Cam.ScreenPointToRay(rayPoint);
                        pixel += AoRayTrace(ray) / sample / sample;
                    }
                }

                TextureHelper.SetPixel(_renderResult, w, h, pixel);
            }
        }
        TextureHelper.SaveImg(_renderResult, "output.png");
    }

}
