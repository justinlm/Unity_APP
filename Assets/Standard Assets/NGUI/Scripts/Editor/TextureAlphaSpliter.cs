using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 辅助，检测运行时间
/// </summary>
public class Watch
{
    public static readonly Watch instance = new Watch();
    public System.Diagnostics.Stopwatch sw { get; private set; }
    private Watch()
    {
        sw = new System.Diagnostics.Stopwatch();
    }
    [System.Diagnostics.Conditional("UNITY_EDITOR1")]
    public static void Start(string format,params object[] args)
    {
        if (!string.IsNullOrEmpty(format))
        {
            var msg = format;
            if (args != null & args.Length > 0)
                msg = string.Format(format, args);
            Debug.Log("----------------------"+msg);
        }
        instance.sw.Reset();
        instance.sw.Start();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR1")]
    public static void Stop()
    {
        instance.sw.Stop();
        Debug.Log("----------------------执行完毕，时间(秒)为:" + instance.sw.Elapsed.TotalSeconds);
    }
}
public static class TextureAlphaSpliter
{
   
    const string
        RGBEndName = "(rgb)",
        AlphaEndName = "(a)";


    public static bool WhetherSplit = true;
    public static bool AlphaHalfSize;

    static Texture s_rgba;

    #region 读写rgba原图
    static string SpliterPath = Application.dataPath + "/Res/UI/Atlas/";
    static string RgbaPath = Application.dataPath + "/Res/UI/Rgba/";

    public static bool UseRgbaTex(UIAtlas atlas)
    {
        var tex = atlas.texture;
        bool isRGB = tex != null && tex.name.Contains("(rgb)");

        if (!isRGB) return false;
        var rgbaTex = GetUIRgbaTex(atlas.texture);
        if (rgbaTex != null)
        {
            atlas.spriteMaterial.SetTexture("_MainTex", rgbaTex);
            return true;
        }
        return false;
    }
    public static Texture2D GetUIRgbaTex(Texture rgbTex)
    {
        if (rgbTex == null)
            return null;

        CachedAtlasRgbaDir();
        
        var texName = rgbTex.name.Replace(RGBEndName, "");
        var path = Path.Combine(RgbaPath, texName);
        path = GetRelativeAssetPath(path)+".png";
        var srcTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return srcTex;
    }

    private static void CachedAtlasRgbaDir()
    {
        if (!Directory.Exists(RgbaPath)) { 
            Directory.CreateDirectory(RgbaPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }

    public static bool SaveUIRgbaTexture(string fromPath)
    {
        var fileName = Path.GetFileName(fromPath);
        //耗时
        CachedAtlasRgbaDir();

        var toPath = Path.Combine(RgbaPath, fileName);
        toPath = GetRelativeAssetPath(toPath);
        Debug.LogFormat("移动图集(rgba)贴图从{0}至{1}", fromPath, toPath);

        if (fromPath != toPath)
        {
            var result = AssetDatabase.MoveAsset(fromPath, toPath);
            //if(!string.IsNullOrEmpty(result))
            //Debug.LogError(result);
        }
        return true;
    }

    /// <summary>
    /// 获得相对路径
    /// </summary>
    /// <param name="_fullPath"></param>
    /// <returns></returns>
    static string GetRelativeAssetPath(string _fullPath)
    {
        _fullPath = GetRightFormatPath(_fullPath);
        int idx = _fullPath.IndexOf("Assets");
        string assetRelativePath = _fullPath.Substring(idx);
        return assetRelativePath;
    }

    /// <summary>
    /// 转换斜杠
    /// </summary>
    /// <param name="_path"></param>
    /// <returns></returns>
    static string GetRightFormatPath(string _path)
    {
        return _path.Replace("\\", "/");
    }
    #endregion
    public static void SplitAlpha(Texture src, bool alphaHalfSize, out Texture rgb, out Texture alpha)
    {
        if (src == null)
            throw new ArgumentNullException("src");

        CachedAtlasRgbaDir();

        // make it readable
        string srcAssetPath = AssetDatabase.GetAssetPath(src);
        var importer = (TextureImporter)AssetImporter.GetAtPath(srcAssetPath);
        {
            importer.isReadable = true;
            importer.SetPlatformTextureSettings("Standalone", 4096, TextureImporterFormat.ARGB32, 100, true);
            importer.SetPlatformTextureSettings("Android", 4096, TextureImporterFormat.ARGB32, 100, true);
            importer.SetPlatformTextureSettings("iPhone", 4096, TextureImporterFormat.ARGB32, 100, true);
        }
        AssetDatabase.ImportAsset(srcAssetPath);

        alpha = CreateAlphaTexture((Texture2D)src, alphaHalfSize);

        //Watch.Start("创建rgb贴图");
        rgb = CreateRGBTexture((Texture2D)src);
        //Watch.Stop();
    }

    static Texture CreateRGBTexture(Texture2D src)
    {
        if (src == null)
            throw new ArgumentNullException("src");

        Watch.Start("分离rgb贴图并导入");

        var srcPixels = src.GetPixels();
        //var tarPixels = new Color[srcPixels.Length];
        //for (int i = 0; i < srcPixels.Length; i++)
        //{
        //    var p = srcPixels[i];
        //    tarPixels[i] = new Color(p.r, p.g, p.b);
        //}

        Texture2D rgbTex = new Texture2D(src.width, src.height, TextureFormat.RGB24, false);
        rgbTex.SetPixels(srcPixels);
        rgbTex.Apply();

        string rgbPath = GetPath(src, RGBEndName);
        string fullPath = Path.GetFullPath(rgbPath);
        var bytes = rgbTex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Watch.Stop();


        Watch.Start("切换rgb贴图的格式");
        int size = Mathf.Max(src.width, src.height, 32);
        SetSettings(rgbPath, size, TextureImporterFormat.ETC_RGB4, TextureImporterFormat.PVRTC_RGB4);
        Watch.Stop();

        return (Texture)AssetDatabase.LoadAssetAtPath(rgbPath, typeof(Texture));
    }

    static Texture CreateAlphaTexture(Texture2D src, bool alphaHalfSize)
    {
        if (src == null)
            throw new ArgumentNullException("src");

        Watch.Start("分离alpha贴图");
        // create texture
        var srcPixels = src.GetPixels();
        var tarPixels = new Color[srcPixels.Length];
        for (int i = 0; i < srcPixels.Length; i++)
        {
            float r = srcPixels[i].a;
            tarPixels[i] = new Color(r, r, r);
        }

        Texture2D alphaTex = new Texture2D(src.width, src.height, TextureFormat.RGB24, false);
        alphaTex.SetPixels(tarPixels);
        alphaTex.Apply();

        Watch.Stop();

        Watch.Start("写入alpha图片至本地");
        // save
        string saveAssetPath = GetPath(src, AlphaEndName);
        string fullPath = Path.GetFullPath(saveAssetPath);
        var bytes = alphaTex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Watch.Stop();

        Watch.Start("重新设置alpha贴图格式");
        int size = alphaHalfSize ? Mathf.Max(src.width / 2, src.height / 2, 32) : Mathf.Max(src.width, src.height, 32);
        SetSettings(saveAssetPath, size, TextureImporterFormat.ETC_RGB4, TextureImporterFormat.PVRTC_RGB4);
        Watch.Stop();

        return (Texture)AssetDatabase.LoadAssetAtPath(saveAssetPath, typeof(Texture));
    }

    static void SetSettings(string assetPath, int maxSize,
        TextureImporterFormat androidFormat, TextureImporterFormat iosFormat)
    {
        assetPath = GetRelativeAssetPath(assetPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        {
            if (importer == null) Debug.Log(assetPath);

            if (importer.textureType != TextureImporterType.GUI)
            {
                importer.textureType = TextureImporterType.GUI;
                importer.SaveAndReimport();
            }
            //importer.isReadable = false;
            //importer.npotScale = TextureImporterNPOTScale.ToNearest;
            //importer.mipmapEnabled = false;
            //importer.alphaIsTransparency = false;
            //importer.wrapMode = TextureWrapMode.Clamp;
            //importer.filterMode = FilterMode.Bilinear;
            //importer.anisoLevel = 4;
            //耗时太多
            //importer.SetPlatformTextureSettings("Android", maxSize, androidFormat, 100, true);
            //importer.SetPlatformTextureSettings("iPhone", maxSize, iosFormat, 100, true);
            //importer.SetPlatformTextureSettings("Standalone", maxSize, TextureImporterFormat.ARGB32, 100, true);

        }
        //AssetDatabase.ImportAsset(assetPath);
    }

    static string GetPath(Texture src, string endName)
    {
        if (src == null)
            throw new ArgumentNullException("src");

        string srcAssetPath = AssetDatabase.GetAssetPath(src);
        if (string.IsNullOrEmpty(srcAssetPath))
            return null;

        string dirPath = GetRelativeAssetPath(SpliterPath);//Path.GetDirectoryName(srcAssetPath);
        string ext = Path.GetExtension(srcAssetPath);
        string fileName = Path.GetFileNameWithoutExtension(srcAssetPath);

        if (fileName.EndsWith(RGBEndName))
            fileName = fileName.Substring(0, fileName.Length - RGBEndName.Length);

        if (fileName.EndsWith(AlphaEndName))
            fileName = fileName.Substring(0, fileName.Length - AlphaEndName.Length);

        return string.Format("{0}{1}{2}{3}", dirPath, fileName, endName ?? "", ext);
    }

    public static Texture GetRGBA(Texture src)
    {
        if (src != null && (s_rgba == null || s_rgba.name != src.name))
        {
            string path = GetPath(src, "");
            if (!string.IsNullOrEmpty(path))
                s_rgba = AssetDatabase.LoadAssetAtPath(path, typeof(Texture)) as Texture;
        }

        return s_rgba;
    }
}
