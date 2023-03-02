using System;
using System.ComponentModel;
using System.Numerics;
using OpenTK.Mathematics;

namespace rt004;



/// <summary>
/// The idea is that the scene supports multiple objects and cameras. There is always a current camera,
/// and a current object, and you can switch between them. Operations can only be done on the current ones.
/// </summary>


public class Scene
{
    public Vector3d LightSourcePosition;
    public Vector3d WorldUpDirection;
	public Camera Cam;
	public Object Object;
	public int ObjectCount { get { return Objects.Count(); } }
	public int CamCount { get { return Cameras.Count(); } }	


    private List<SceneObject> Objects;
    private List<Camera> Cameras;

	/// <summary>
	/// Emppty constructor, so Scene can be initialized without config.
	/// </summary>
	public Scene()
	{
        Objects = new List<SceneObject>();
		Cameras = new List<Camera>();
    }

    /// <summary>
    /// Set up basic properties of the scene, like Initialize the camera from the config file.
    /// </summary>
    public Scene(Config config)
	{
		Objects= new List<SceneObject>();
        Cameras = new List<Camera>
		{
			new PerspectiveCamera(config.CameraConfig,config.GeneralConfig.width,config.GeneralConfig.height)
		};
        Cam = Cameras[0];
        float[] v = config.SceneConfig.WorldUpDirection;
		float[] l = config.SceneConfig.LightSourcePosition;
		WorldUpDirection = new Vector3d(v[0], v[1], v[2]);
		LightSourcePosition = new Vector3d(l[0], l[1], l[2]);
	}

	public void AddObject()
	{

	}

	public void RemoveObject()
	{

	}

	public void SwapCamera(int cam_ID)
	{
		Cam = Cameras[cam_ID];
	}

	public void SwapObject(int obj_ID)
	{
		Object = Objects[obj_ID];
	}
	
	public void GenerateImge(int width, int height)
	{
		
	}
}


public interface SceneEntity
{
    public Vector3d Position { get; set; }
    public void Translate(Vector3d translation);
	public void Rotate(Vector3d rotation);
}

/// <summary>
/// Includes the type of projection, and plane properties (width, height, and distance from the camera).
/// </summary>
public interface Projection
{
	public int PlaneWidth { get; set; }
	public int PlaneHeight { get; set; }
	public float PlaneDistance { get; set; }
	public float GetPixel(Vector3d RayDirection);
}



public class Camera:SceneEntity
{

	public float AzimuthAngle;
	public float ElevationAngle;
    public Vector3d ViewDirection { 
		get { return new Vector3d(vd[0], vd[1], vd[2]); } 
		set 
		{
			vd[0] = (float)value.X;
            vd[1] = (float)value.Y;
            vd[2] = (float)value.Z;
        }
	}
    public Vector3d Position
    {
        get { return new Vector3d(pos[0], pos[1], pos[2]); }
        set
        {
            pos[0] = (float)value.X;
            pos[1] = (float)value.Y;
            pos[2] = (float)value.Z;
        }
    }

    private float[] vd=new float[3];
	private float[] pos = new float[3];

	public Camera(CameraConfig config)
	{
		vd = config.ViewDirection;
		pos = config.Position;
		AzimuthAngle = config.Azimuth;
		ElevationAngle = config.ElevationAngle;
	}

	public void Translate(Vector3d Translation)
	{

	}

	public void Rotate(Vector3d Rotation)
	{

	}
	public void SetFOV(float Distance) { }
}

public class PerspectiveCamera:Camera,Projection
{
	public float PlaneDistance { get; set; }
	public int PlaneWidth
    {
        get { return width; }
        set { width = value; }
    }

    public int PlaneHeight 
	{ 
		get { return height; } 
		set { height = value; } 
	}

	private int width;
	private int height;
	
    public PerspectiveCamera(CameraConfig config, int w, int h) :base(config)
	{
		width = w;
		height = h;
		PlaneDistance = config.ViewPlaneDistance;
	}
	public float GetPixel(Vector3d RayDirection)
	{
		throw new NotImplementedException();
	}
}


/// <summary>
/// Won't be implemented for a while, only if I have time.
/// </summary>
public class FisheyeLense :Camera,Projection
{

	public float PlaneDistance { get; set; }
    public int PlaneWidth
    {
        get { return width; }
        set { width = value; }
    }

    public int PlaneHeight
    {
        get { return height; }
        set { height = value; }
    }

    private int width;
    private int height;
    public FisheyeLense(CameraConfig config, int w, int h) :base(config)
	{
		width = w;
		height = h;
		PlaneDistance = config.ViewPlaneDistance;
    }
    public float GetPixel(Vector3d RayDirection)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// Represents an SceneObject in the scene. Going to be a tree structure, that stores the vertices, edges, planes etc...
/// </summary>
public class SceneObject:SceneEntity
{
	public Vector3d Position { get; set; }
    
    public void Translate(Vector3d translation)
    {
        throw new NotImplementedException();
    }


    public void Rotate(Vector3d rotation)
    {
        throw new NotImplementedException();
    }
}
