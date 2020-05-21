using UnityEngine;
using System.Collections;
using System.IO;


public static class TextureHelper
{

    public static void SetPixel(Texture2D tex, int x, int y, float r, float g, float b)
    {
        tex.SetPixel(x, y, new Color(r, g, b));
    }

    public static void SetPixel(Texture2D tex, int row, int col, Color c)
    {
        tex.SetPixel(row, col, c);
    }

    public static void SaveImg(Texture2D tex, string path)
    {
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Application.dataPath, path), bytes);
    }
}