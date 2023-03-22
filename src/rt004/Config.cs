using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTK.Mathematics;

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
    public ILogger logger = Logging.CreateLogger<SceneConfig>();
    public float[] WorldUpDirection { get; set; }
    public bool Shadows { get; set; }

    public readonly List<SceneObject> SceneObjects=new List<SceneObject>();
    public readonly List<LightSource> LightSources = new List<LightSource>();
    public void Init()
    {
        try
        {
            if (Objects.Spheres.Number != Objects.Spheres.Positions.Count())
                logger.LogWarning("Number of denoted spheres is different than number of positions!");
            for (int i = 0; i < Objects.Spheres.Number; i++)
            {
                SceneObjects.Add(new Sphere(
                                ColorTools.ArrToV3d(Objects.Spheres.Colors[i]),
                                ColorTools.ArrToV3d(Objects.Spheres.Positions[i])
                                , new Phong(Objects.Spheres.Materials[i][0], Objects.Spheres.Materials[i][1], Objects.Spheres.Materials[i][2], Objects.Spheres.Materials[i][3])
                                , Objects.Spheres.Radiuses[i]));
            }
            if (Objects.Planes.Number != Objects.Planes.Positions.Count())
                logger.LogWarning("Number of denoted planes is different than number of positions!");
            for (int i = 0; i < Objects.Planes.Number; i++)
            {
                SceneObjects.Add(new Plane(ColorTools.ArrToV3d(Objects.Planes.Positions[i])
                                , ColorTools.ArrToV3d(Objects.Planes.Normals[i])
                                , ColorTools.ArrToV3d(Objects.Planes.Colors[i])
                                , new Phong(Objects.Spheres.Materials[i][0], Objects.Spheres.Materials[i][1], Objects.Spheres.Materials[i][2], Objects.Spheres.Materials[i][3])
                                ));
            }
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
    public class Obj
    {
        /// <summary>
        /// Creates objects that are in the scene with the given properties. 
        /// </summary>
        
        public class SphereObjects
        {
            public int Number { get; set; }

            public string Instance = "Sphere";
            public List<float[]> Positions { get; set; }
            public List<float[]> Colors { get; set; }
            public List<float[]> Materials { get; set; }
            public float[] Radiuses { get; set; } 
        }
        public class PlaneObjects
        {
            public string Instance = "Plane";
            public int Number { get; set; }
            public List<float[]> Normals { get; set; }
            public List<float[]> Positions { get; set; }
            public List<float[]> Colors { get; set; }
            public List<float[]> Materials { get; set; }
        }
        public SphereObjects Spheres { get; set; }
        public PlaneObjects Planes { get; set; }

    }
    public class Lights
    {
        public class PointLighings
        {
            public int Number { get; set; }
            public List<float[]> Positions { get; set; }
            public List<float[]> SpecularIntensities { get; set; }
            public List<float[]> DiffuseIntensities { get; set; }
            
        }

        public class DirectionalLightings
        {
            public int Number { get; set; }
            public List<float[]> Directions { get; set; }
            public List<float[]> SpecularIntensities { get; set; }
            public List<float[]> DiffuseIntensities { get; set; }
        }
        public PointLighings PointLights { get; set; }
        public DirectionalLightings DirectionalLights { get; set; }



    }
    public Obj Objects { get; set; }
    public Lights Lightings { get; set; }
    
    public List<float[]> PointLightPositions { get; set; }
    public List<float[]> DirectionalLightDirections { get; set; }
    public float[] AmbientLighting { get; set; }

    

}

public class PlaneConfig
{
    public int Height { get; set; }
    public int Width { get; set; }
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
