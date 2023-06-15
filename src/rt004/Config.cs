using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace rt004;


/// <summary>
/// Config class, that wraps up all of the other configuration types. On calling the constructor, it instantiates all of the
/// other more specific config classes. The other config classes are only used for storing this data.
/// </summary>
public class Config
{
    private ILogger logger=Logging.CreateLogger<Config>();
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
            SceneConfig.Init();
            GeneralConfig = JsonSerializer.Deserialize<GeneralConfig>(genobj);
            PlaneConfig = JsonSerializer.Deserialize<PlaneConfig>(planeobj);
        }
        catch (NullReferenceException)
        {
            logger.LogCritical("One of the configs initialized as null, config file is not set up correctly. Exiting.");
            Environment.Exit(1);
        }
    }   
}

/// <summary>
/// Set up getters and setters later if necessary. General config will probably 
/// won't be used later.
/// </summary>
public class GeneralConfig
{   
    public string FileName { get; set; } 
    public string FileNameRaytraced { get; set; }
    public string Color1{ get; set; }
    public string Color2 { get; set; }
    public bool Parallel { get; set; }
}

public class CameraConfig 
{
    public double[] Position { get; set; }
    public double[] Target { get; set; }
    public double FOV { get; set; }
}

public class SceneConfig
{
    public ILogger logger = Logging.CreateLogger<SceneConfig>();
    public double[] WorldUpDirection { get; set; }
    public bool Shadows { get; set; }
    public int MaxDepth { get; set; }
    public double[] BackgroundColor { get; set; }
    public string SceneGraph { get; set; }

    public readonly List<LightSource> LightSources = new List<LightSource>();
    public void Init()
    {
        try
        {
            if (Lightings.PointLights.Number != Lightings.PointLights.Positions.Count())
                logger.LogWarning("Number of denoted point lightings is different than number of positions!");
            for (int i=0; i<Lightings.PointLights.Number;i++)
            {
                LightSources.Add(new PointLight(
                    ColorTools.ArrToV3d(Lightings.PointLights.Positions[i]), 
                    ColorTools.ArrToV3d(Lightings.PointLights.DiffuseIntensities[i]),
                    ColorTools.ArrToV3d(Lightings.PointLights.SpecularIntensities[i])
                    ));
            }
            if(Lightings.DirectionalLights.Number != Lightings.DirectionalLights.Directions.Count())
                logger.LogWarning("Number of denoted point lightings is different than number of positions!");
            for (int i = 0; i < Lightings.DirectionalLights.Number; i++)
            {
                LightSources.Add(new PointLight(
                    ColorTools.ArrToV3d(Lightings.DirectionalLights.Directions[i]),
                    ColorTools.ArrToV3d(Lightings.DirectionalLights.DiffuseIntensities[i]),
                    ColorTools.ArrToV3d(Lightings.DirectionalLights.SpecularIntensities[i])
                    ));
            }
        }
        catch (IndexOutOfRangeException)
        {
            logger.LogWarning("Not all object's attributes were specified in config file properly. Not all objects were added to the scene.");
        }
    }
    
    public class Lights
    {
        public class PointLighings
        {
            public int Number { get; set; }
            public List<double[]> Positions { get; set; }
            public List<double[]> SpecularIntensities { get; set; }
            public List<double[]> DiffuseIntensities { get; set; }   
        }

        public class DirectionalLightings
        {
            public int Number { get; set; }
            public List<double[]> Directions { get; set; }
            public List<double[]> SpecularIntensities { get; set; }
            public List<double[]> DiffuseIntensities { get; set; }
        }
        public PointLighings PointLights { get; set; }
        public DirectionalLightings DirectionalLights { get; set; }

    }
    public Lights Lightings { get; set; }
    
    public List<double[]> PointLightPositions { get; set; }
    public List<double[]> DirectionalLightDirections { get; set; }
    public double[] AmbientLighting { get; set; }

    

}

public class PlaneConfig
{
    public int Height { get; set; }
    public int Width { get; set; }
    public int RayPerPixel { get; set; }
}

public static class Logging
{
    public static ILogger CreateLogger<T>()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole(options => { options.TimestampFormat = "[HH:mm:ss] "; }).AddDebug().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<T>();

        return logger;
    }

}
