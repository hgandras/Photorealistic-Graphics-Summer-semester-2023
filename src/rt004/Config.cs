using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace rt004;


/// <summary>
/// This is just a basic set up, probably I will rewrite some parts in the future. 
/// </summary>
public class Config
{
    
}

/// <summary>
/// Set up getters and setters later if necessary.
/// </summary>
public class GeneralConfig: Config
{
    public int width { get; set; }
    public int height { get; set; }
    public string fileName { get;   set; } 
    public string color1{ get; set; }
    public string color2 { get; set; }

    public static GeneralConfig Init(string ConfigFile)
    {
        string jsonString = File.ReadAllText(ConfigFile);
        var Obj = JsonDocument.Parse(jsonString);
        var obj = Obj.RootElement.GetProperty("GeneralConfig");
        var config = JsonSerializer.Deserialize<GeneralConfig>(obj);

        return config;
    }
}

public class CameraConfig :Config
{
    public float[] pos { get; set; }

    public float[,] orientation { get; set; }


    public static CameraConfig Init(string ConfigFile)
    {
        string jsonString = File.ReadAllText(ConfigFile);
        var Obj = JsonDocument.Parse(jsonString);
        var obj = Obj.RootElement.GetProperty("CameraConfig");
        var config = JsonSerializer.Deserialize<CameraConfig>(obj);

        return config;
    }
}
