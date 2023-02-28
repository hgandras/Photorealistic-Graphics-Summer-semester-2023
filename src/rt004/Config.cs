using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace rt004;


/// <summary>
/// Config class, that wraps up all of the other configuration types. On calling the constructor, it instantiates all of the
/// other more specific config classes. The other config classes are only used for storing this data.
/// </summary>
public class Config
{
    public CameraConfig CameraConfig;
    public SceneConfig SceneConfig;
    public GeneralConfig GeneralConfig;

    public Config(string ConfigFile) 
    {
        string jsonString = File.ReadAllText(ConfigFile);
        var Obj = JsonDocument.Parse(jsonString);
        var camobj = Obj.RootElement.GetProperty("CameraConfig");
        var sceneobj = Obj.RootElement.GetProperty("SceneConfig");
        var genobj = Obj.RootElement.GetProperty("GeneralConfig");
        try
        {
            CameraConfig = JsonSerializer.Deserialize<CameraConfig>(camobj);
            SceneConfig = JsonSerializer.Deserialize<SceneConfig>(sceneobj);
            GeneralConfig = JsonSerializer.Deserialize<GeneralConfig>(genobj);
        }
        catch (NullReferenceException)
        {
            //Will be modified later to be more verbose
            Console.WriteLine("Config file is not correct");
        }
    }   
}

/// <summary>
/// Set up getters and setters later if necessary.
/// </summary>
public class GeneralConfig
{   
    public int width { get; set; }
    public int height { get; set; }
    public string fileName { get;   set; } 
    public string color1{ get; set; }
    public string color2 { get; set; }
}

public class CameraConfig 
{
    public float[] Position { get; set; }
    public float[] ViewDirection { get; set; }
    public float ElevationAngle { get; set; }
    public float Azimuth { get; set; }
    public string PrejectionType { get; set; }
    public float ViewPlaneDistance { get; set; } 
}

public class SceneConfig
{
    public float[] WorldUpDirection { get; set; }

}
