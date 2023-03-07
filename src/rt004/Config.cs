using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenTK.Mathematics;

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
    public PlaneConfig PlaneConfig;

    public Config(string ConfigFile) 
    {
        string jsonString = File.ReadAllText(ConfigFile);
        var Obj = JsonDocument.Parse(jsonString);
        var camobj = Obj.RootElement.GetProperty("CameraConfig");
        var sceneobj = Obj.RootElement.GetProperty("SceneConfig");
        var genobj = Obj.RootElement.GetProperty("GeneralConfig");
        var planeobj = Obj.RootElement.GetProperty("PlaneConfig");
        try
        {
            CameraConfig = JsonSerializer.Deserialize<CameraConfig>(camobj);
            SceneConfig = JsonSerializer.Deserialize<SceneConfig>(sceneobj);
            GeneralConfig = JsonSerializer.Deserialize<GeneralConfig>(genobj);
            PlaneConfig = JsonSerializer.Deserialize<PlaneConfig>(planeobj);
        }
        catch (NullReferenceException)
        {
            //Will be modified later to be more verbose
            Console.WriteLine("Config file is not correct");
        }
    }   
}

/// <summary>
/// Set up getters and setters later if necessary. General config will probably 
/// won't be used later.
/// </summary>
public class GeneralConfig
{   
    public string fileName { get;   set; } 
    public string color1{ get; set; }
    public string color2 { get; set; }
}

public class CameraConfig 
{
    public float[] Position { get; set; }
    public float[] Target { get; set; }
    public float FOV { get; set; }
}

public class SceneConfig
{
    public float[] WorldUpDirection { get; set; }
    public float[] LightSourcePosition { get; set; }
    public List<string> Objects { get; set; }
    public List<float[]> ObjectColors { get; set; }
    public List<float[]> ObjectPositions { get; set; }

}

public class PlaneConfig
{
    public int Height { get; set; }
    public int Width { get; set; }
}
