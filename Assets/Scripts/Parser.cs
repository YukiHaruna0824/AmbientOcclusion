using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class OutputInfo
{
    public Vector2 Resolution { get; set; }
    public string ImageName { get; set; }
    public string Sort { get; set; }

    public OutputInfo()
    {
        Resolution = Vector2.zero;
        ImageName = "";
        Sort = "";
    }

    public OutputInfo(Vector2 _resolution, string _imageName, string _sort)
    {
        Resolution = _resolution;
        ImageName = _imageName;
        Sort = _sort;
    }
    
    public void Init()
    {
        Resolution = Vector2.zero;
        ImageName = "";
        Sort = "";
    }
}

public enum ObjectType
{
    none, sphere, cylinder, cone, plane, mesh
}

public enum LightType
{
    none, point, area
}

public enum MaterialType
{
    none, mirror, map, kdks, glass
}

public class MaterialAttribute
{
    public Vector3 kd;
    public Vector3 ks;

    //for color, bump map
    public string cmapName;
    public string bmapName;

    //for mesh
    public string meshName;

    public MaterialAttribute()
    {
        kd = Vector3.zero;
        ks = Vector3.zero;
        cmapName = "";
        bmapName = "";
        meshName = "";
    }
}

public class RayTracingObject
{
    public ObjectType o_type = ObjectType.none;
    public LightType l_type = LightType.none;

    public MaterialType m_type = MaterialType.none;

    public Vector3 pos;
    public Vector3 rot;
    public Vector3 scale;

    // width, height, radius
    public Vector3 whrInfo;

    //for cylinder ymin, ymax
    public Vector2 yminmax;

    // for material attribute
    public MaterialAttribute mattr;

    //for light
    public Vector3 lightColor;

    //for area light
    public int nsample;

    public RayTracingObject()
    {
        o_type = ObjectType.none;
        l_type = LightType.none;
        m_type = MaterialType.none;
        pos = Vector3.zero;
        rot = Vector3.zero;
        scale = Vector3.one;
        whrInfo = Vector3.zero;
        yminmax = Vector2.zero;
        mattr = new MaterialAttribute();
        lightColor = Vector3.zero;
        nsample = 0;
    }

}

public class RayTracingInfo
{
    //Sample Info
    public string sampleMethod;
    public int sampleCount;     //sample counts with every pixel

    //Camera Info
    public Vector3 cameraPos;
    public Vector3 cameraCenter;
    public Vector3 cameraUp;

    // 0 : perspective, 1 : orthographic
    public int cameraType;
    public float cameraFov;

    public List<RayTracingObject> objects;

    public RayTracingInfo()
    {
        sampleMethod = "";
        sampleCount = 0;
        cameraPos = Vector3.zero;
        cameraCenter = Vector3.zero;
        cameraUp = Vector3.zero;
        cameraType = 0;
        cameraFov = 0;
        objects = new List<RayTracingObject>();
    }

    public void Init()
    {
        sampleMethod = "";
        sampleCount = 0;
        cameraPos = Vector3.zero;
        cameraCenter = Vector3.zero;
        cameraUp = Vector3.zero;
        cameraType = 0;
        cameraFov = 0;
        objects.Clear();
    }
}

public class KeyWord
{
    public const string K_XResolution = "\"integer xresolution\" ";
    public const string K_YResolution = "\"integer yresolution\" ";
    public const string K_OutputName = "\"string filename\" ";
    public const string K_SamplerMethod = "\"integer pixelsamples\" ";
    public const string K_CameraType = "\"perspective\" ";
    public const string K_CameraFov = "\"float fov\" ";

    public const string K_WorldBegin = "WorldBegin";
    public const string K_WorldEnd = "WorldEnd";
    public const string K_AttributeBegin = "AttributeBegin";
    public const string K_AttributeEnd = "AttributeEnd";

    public const string K_Translate = "Translate";
    public const string K_Rotate = "Rotate";
    public const string K_Scale = "Scale";
    public const string K_Shape = "Shape";
    public const string K_Material = "Material";
    public const string K_Include = "Include";
    public const string K_LightSource = "LightSource";
}

public class Parser
{
    //Parse Info
    public OutputInfo outputInfo = new OutputInfo();
    public RayTracingInfo rayTracingInfo = new RayTracingInfo();

    private bool _debug = false;
    private List<string> _records = new List<string>();
    private int _curIndex = 0;
    private string _rootFolder;

    // read cpbrt file
    private bool ReadFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        char[] charsToTrim = { ' ', '\t' };

        using (StreamReader sr = new StreamReader(filePath))
        {
            while(sr.Peek() >= 0)
            {
                //filter '\t' and comment info
                string line = sr.ReadLine().Trim(charsToTrim);
                if (line != "" && !line.StartsWith("#"))
                    _records.Add(line);
            }
        }

        return true;
    }

    //clear "" or []
    private string ExtractString(string s)
    {
        char[] charsToTrim = { '"', '[', ']' };
        return s.Trim(charsToTrim);
    }

    //clear attribute keyword space 
    private string FormatAttributeInfo(string s)
    {
        string[] shapeKeywords = { "\"float radius\"", "\"float ymin\"", "\"float ymax\"", "\"float width\"", "\"float height\"" };
        string[] lightKeywords = { "\"color L\"", "\"point from\"", "\"integer nsamples\"" };
        string[] materialKeywords = { "\"color map\"", "\"bump map\"", "\"color Kd\"", "\"color Ks\"" }; 
        foreach(string shapeKeyword in shapeKeywords)
            s = s.Replace(shapeKeyword, shapeKeyword.Replace(" ", ""));
        foreach (string lightKeyword in lightKeywords)
            s = s.Replace(lightKeyword, lightKeyword.Replace(" ", ""));
        foreach (string materialKeyword in materialKeywords)
            s = s.Replace(materialKeyword, materialKeyword.Replace(" ", ""));
        return s;
    }

    private bool ParseFilm()
    {
        //Parse output sort
        string line = _records[_curIndex++];
        if (!line.StartsWith("Film"))
            return false;

        string[] tokens = line.Split(' ');
        outputInfo.Sort = ExtractString(tokens[1]);

        //Parse output resolution
        line = _records[_curIndex++];
        if (!line.StartsWith(KeyWord.K_XResolution))
            return false;

        line = line.Replace(KeyWord.K_XResolution, "");
        line = line.Replace(KeyWord.K_YResolution, "");
        tokens = line.Split(' ');
        outputInfo.Resolution = new Vector2(int.Parse(ExtractString(tokens[0])), int.Parse(ExtractString(tokens[1])));

        //Parse output imagename
        line = _records[_curIndex++];
        if (!line.StartsWith(KeyWord.K_OutputName))
            return false;
        line = line.Replace(KeyWord.K_OutputName, "");
        outputInfo.ImageName = ExtractString(line);

        return true;
    }

    private bool ParseCameraInfo()
    {
        //Parse sample method and sample count
        string line = _records[_curIndex++];
        if (!line.StartsWith("Sampler"))
            return false;
        line = line.Replace(KeyWord.K_SamplerMethod, "");
        string[] tokens = line.Split(' ');
        rayTracingInfo.sampleMethod = ExtractString(KeyWord.K_SamplerMethod.TrimEnd());
        rayTracingInfo.sampleCount = int.Parse(ExtractString(tokens[1]));

        //Parse Camera Info
        line = _records[_curIndex++];
        if (!line.StartsWith("LookAt"))
            return false;
        tokens = line.Split(' ');
        rayTracingInfo.cameraPos = new Vector3(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));
        rayTracingInfo.cameraCenter = new Vector3(float.Parse(tokens[4]), float.Parse(tokens[5]), float.Parse(tokens[6]));
        rayTracingInfo.cameraUp = new Vector3(float.Parse(tokens[7]), float.Parse(tokens[8]), float.Parse(tokens[9]));

        line = _records[_curIndex++];
        if (!line.StartsWith("Camera"))
            return false;
        line = line.Replace(KeyWord.K_CameraType, "");
        line = line.Replace(KeyWord.K_CameraFov, "");
        tokens = line.Split(' ');
        rayTracingInfo.cameraType = 0;
        rayTracingInfo.cameraFov = float.Parse(ExtractString(tokens[1]));

        return true;
    }

    private bool ParseObject()
    {
        RayTracingObject robj = new RayTracingObject();

        for(int i = _curIndex; i < _records.Count; i++)
        {
            string line = _records[i];
            string[] tokens;
            if (line == KeyWord.K_WorldBegin)
                continue;
            else if (line == KeyWord.K_WorldEnd)
                continue;
            else if (line == KeyWord.K_AttributeBegin)
                robj = new RayTracingObject();
            else if (line == KeyWord.K_AttributeEnd)
                rayTracingInfo.objects.Add(robj);
            else if (line.StartsWith(KeyWord.K_Translate))
            {
                tokens = line.Split(' ');
                robj.pos = new Vector3(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));
            }
            else if (line.StartsWith(KeyWord.K_Rotate))
            {
                tokens = line.Split(' ');
                float mult = float.Parse(tokens[1]);
                //blender to unity yz opposite
                robj.rot = new Vector3(float.Parse(tokens[2]), float.Parse(tokens[4]), float.Parse(tokens[3])) * mult;
            }
            else if (line.StartsWith(KeyWord.K_Scale))
            {
                tokens = line.Split(' ');
                robj.scale = new Vector3(float.Parse(tokens[1]), float.Parse(tokens[2]), float.Parse(tokens[3]));
            }
            else if (line.StartsWith(KeyWord.K_Shape))
            {
                line = FormatAttributeInfo(line);
                tokens = line.Split(' ');
                for(int j = 0; j < tokens.Length; j+=2)
                {
                    string token = tokens[j];
                    //none, sphere, cylinder, cone, plane, mesh
                    if (token == "Shape")
                    {
                        if (tokens[j + 1] == "\"sphere\"")
                            robj.o_type = ObjectType.sphere;
                        else if (tokens[j + 1] == "\"cylinder\"")
                            robj.o_type = ObjectType.cylinder;
                        else if (tokens[j + 1] == "\"cone\"")
                            robj.o_type = ObjectType.cone;
                        else if (tokens[j + 1] == "\"plane\"")
                            robj.o_type = ObjectType.plane;
                        else
                        {
                            if (_debug)
                                Debug.Log("Invalid Shape Type!");
                            return false;
                        }
                    }
                    else if(token == "\"floatradius\"")
                        robj.whrInfo.z = float.Parse(ExtractString(tokens[j + 1]));
                    else if (token == "\"floatymin\"")
                        robj.yminmax.x = float.Parse(ExtractString(tokens[j + 1]));
                    else if (token == "\"floatymax\"")
                        robj.yminmax.y = float.Parse(ExtractString(tokens[j + 1]));
                    else if (token == "\"floatwidth\"")
                        robj.whrInfo.x = float.Parse(ExtractString(tokens[j + 1]));
                    else if (token == "\"floatheight\"")
                        robj.whrInfo.y = float.Parse(ExtractString(tokens[j + 1]));
                    else
                    {
                        if (_debug)
                            Debug.Log("Invalid Shape Attribute!");
                        return false;
                    }
                }
            }
            else if (line.StartsWith(KeyWord.K_Material))
            {
                line = FormatAttributeInfo(line);
                tokens = line.Split(' ');
                for (int j = 0; j < tokens.Length; j++)
                {
                    string token = tokens[j];
                    if (token == "Material")
                    {
                        if (tokens[j + 1] == "\"mirror\"")
                        {
                            robj.m_type = MaterialType.mirror;
                            j++;
                        }
                        else if (tokens[j + 1] == "\"glass\"")
                        {
                            robj.m_type = MaterialType.glass;
                            j++;
                        }
                    }
                    else if (token == "\"colormap\"")
                    {
                        robj.m_type = MaterialType.map;
                        robj.mattr.cmapName = Path.Combine(_rootFolder, ExtractString(tokens[++j]));
                    }
                    else if (token == "\"bumpmap\"")
                    {
                        robj.m_type = MaterialType.map;
                        robj.mattr.bmapName = Path.Combine(_rootFolder, ExtractString(tokens[++j]));
                    }
                    else if (token == "\"colorKd\"")
                    {
                        robj.m_type = MaterialType.kdks;
                        robj.mattr.kd = new Vector3(float.Parse(ExtractString(tokens[++j])), float.Parse(tokens[++j])
                            , float.Parse(ExtractString(tokens[++j])));
                    }
                    else if (token == "\"colorKs\"")
                    {
                        robj.m_type = MaterialType.kdks;
                        robj.mattr.ks = new Vector3(float.Parse(ExtractString(tokens[++j])), float.Parse(tokens[++j])
                            , float.Parse(ExtractString(tokens[++j])));
                    }
                    else
                    {
                        if (_debug)
                            Debug.Log("Invalid Material Attribute!");
                        return false;
                    }
                }

            }
            else if (line.StartsWith(KeyWord.K_Include))
            {
                tokens = line.Split(' ');
                robj.o_type = ObjectType.mesh;
                robj.mattr.meshName = Path.Combine(_rootFolder, ExtractString(tokens[1]));
            }
            else if (line.StartsWith(KeyWord.K_LightSource))
            {
                line = FormatAttributeInfo(line);
                tokens = line.Split(' ');
                for(int j = 0; j < tokens.Length; j++)
                {
                    string token = tokens[j];
                    if (token == "LightSource")
                    {
                        if (tokens[j + 1] == "\"point\"")
                            robj.l_type = LightType.point;
                        else if (tokens[j + 1] == "\"area\"")
                            robj.l_type = LightType.area;
                        else
                        {
                            if (_debug)
                                Debug.Log("Invalid Light Type!");
                            return false;
                        }
                        j++;
                    }
                    else if (token == "\"colorL\"")
                        robj.lightColor = new Vector3(float.Parse(ExtractString(tokens[++j]))/15f, float.Parse(tokens[++j])/15f
                            , float.Parse(ExtractString(tokens[++j]))/15f);
                    else if (token == "\"pointfrom\"")
                        robj.pos = new Vector3(float.Parse(ExtractString(tokens[++j])), float.Parse(tokens[++j])
                            , float.Parse(ExtractString(tokens[++j])));
                    else if (token == "\"integernsamples\"")
                        robj.nsample = int.Parse(ExtractString(tokens[++j]));
                    else if (token == "\"floatwidth\"")
                        robj.whrInfo.x = float.Parse(ExtractString(tokens[++j]));
                    else if (token == "\"floatheight\"")
                        robj.whrInfo.y = float.Parse(ExtractString(tokens[++j]));
                    else
                    {
                        if (_debug)
                            Debug.Log("Invalid Material Attribute!");
                        return false;
                    }
                }
            }
            else
            {
                if (_debug)
                    Debug.Log("Invalid Attribute!");
                return false;
            }
        }
        return true;
    }

    private void Init()
    {
        outputInfo.Init();
        rayTracingInfo.Init();
        _records.Clear();
        _curIndex = 0;
        _rootFolder = "";
    }

    public bool Parse(string rootFolder, string filePath)
    {
        Init();
        _rootFolder = rootFolder;

        if (!ReadFile(filePath))
        {
            if (_debug)
                Debug.Log("Open File Failed!");
            return false;
        }

        if(!ParseFilm())
        {
            if (_debug)
                Debug.Log("Parse Film Failed!");
            return false;
        }

        if (!ParseCameraInfo())
        {
            if (_debug)
                Debug.Log("Parse Camera Failed!");
            return false;
        }

        if (!ParseObject())
        {
            if (_debug)
                Debug.Log("Parse Object Failed!");
            return false;
        }

        if (_debug)
        {
            Debug.Log(rayTracingInfo.objects.Count);
            Debug.Log("succeed");
        }

        return true;
    }
}
