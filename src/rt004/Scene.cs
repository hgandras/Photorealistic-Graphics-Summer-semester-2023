using System;
using System.ComponentModel;
using System.Numerics;
using OpenTK.Mathematics;

namespace rt004;



/// <summary>
/// The idea is that the scene supports multiple objects and cameras. There is always a current camera,
/// and a current object, and you can switch between them. Operations can only be done on the current ones.
/// 
/// During projection, everything is going to be calculated in the camera's coordinate system, so objects and light
/// source coordinates all will have to be transformed.
/// </summary>

/*
 * TODO: Create empty camera type constructors
 * TODO: Init value of the Object variable, so its not null upon exiting the Scene constructor
 */

public class Scene
{
	public Vector3d LightSourcePosition;
	public Vector3d WorldUpDirection;
	public Camera Cam;
	public Object Object;
	public Plane Plane;
	public int ObjectCount { get { return Objects.Count(); } }
	public int CamCount { get { return Cameras.Count(); } }
	public int PlaneWidthPx;
	public int PlaneHeightPx;


	private List<SceneObject> Objects;
	private List<Camera> Cameras;

	/// <summary>
	/// Empty constructor, so Scene can be initialized without config.
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
			new PerspectiveCamera(config.CameraConfig)
		};
        Cam = Cameras[0];
        float[] v = config.SceneConfig.WorldUpDirection;
		float[] l = config.SceneConfig.LightSourcePosition;
		WorldUpDirection = new Vector3d(v[0], v[1], v[2]);
		LightSourcePosition = new Vector3d(l[0], l[1], l[2]);
		Plane = new Plane(config.PlaneConfig);
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
	
	/// <summary>
	/// For each pixel in the plane get as many sample points as it is defined by the camera's property,
	/// and based on the sample points, generate the ray direction vectors. After that, for all rays, calculate 
	/// the intersection of all objects in the scene, and based on that the reflection, and the pixel color value
	/// in float.
	/// </summary>
	/// <param name="width"></param>
	/// <param name="height"></param>
	public void GenerateImage()
	{
		for(int h=0;h<Plane.PxHeight;h++)
		{
			for(int w=0;w<Plane.PxWidth;w++)
			{
				Vector3d ray=Cam.CastRay(Plane, w, h);
			}
		}
	}
}

/// <summary>
/// Represents movable objects in the scene like light sources, objects, cameras. 
/// </summary>
public abstract class SceneEntity
{
    public abstract Vector3d Position { get; set; }
    public abstract void Translate(Vector3d translation);
	public abstract void Rotate(Vector3d rotation);
}

/// <summary>
/// Includes the type of projection, and plane properties (width, height, and distance from the camera).
/// 
/// </summary>
public class Plane
{
	public float DistanceFromCamera;
	public int PxWidth;
	public int PxHeight;

	public Plane(PlaneConfig config)
	{
		DistanceFromCamera = config.DistanceFromCamera;
		PxHeight = config.Height;
		PxWidth = config.Width;
	}
}

public interface Projection
{
	public Vector3d CastRay(Plane plane,int px_x,int px_y);
}

public class Camera:SceneEntity,Projection
{	
    public int SamplesPerPixel; //Just an idea, might be used later.
    public Vector3d ViewDirection { 
		get { return new Vector3d(vd[0], vd[1], vd[2]); } 
		set 
		{
			vd[0] = (float)value.X;
            vd[1] = (float)value.Y;
            vd[2] = (float)value.Z;
        }
	}
    public override Vector3d Position
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
		SamplesPerPixel = config.SamplesPerPx;
	}

	public override void Translate(Vector3d Translation)
	{

	}

	public override void Rotate(Vector3d Rotation)
	{

	}

	public void TransformToCameraSystem(SceneEntity sceneEntity)
	{

	}
	public virtual Vector3d CastRay(Plane plane, int px_x, int px_y)
	{
		return Vector3d.Zero;
	}
	
}

public class PerspectiveCamera:Camera,Projection
{
    public float FOV;
    public PerspectiveCamera(CameraConfig config) :base(config)
	{
		FOV = config.FOV;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="plane">The plane to project to </param>
	/// <param name="px_x"></param>
	/// <param name="px_y"></param>
	/// <returns>The ray's direction vector in the camera coordinate system.</returns>
	/// <exception cref="NotImplementedException"></exception>
	public override Vector3d CastRay(Plane plane, int px_x, int px_y)
	{
		
		//Calculate real size of the plane. Calculations can be faster, if real plane size is precomputed.
		double w =2f* plane.DistanceFromCamera * Math.Tan( (FOV/2f)*Math.PI/180);
		double h = plane.PxHeight * (w /plane.PxWidth);
		
		double w_pixel_step = w / plane.PxWidth;
		double h_pixel_step=h/plane.PxHeight;
		
        double x = px_x * w_pixel_step + w_pixel_step / 2 - w / 2;
		double y = px_y * h_pixel_step + h_pixel_step / 2 - h / 2;
        //The intersected point in the plane in the camera system
        Vector3d intersection_point =new Vector3d(x,y,-plane.DistanceFromCamera);
		
		return Vector3d.Normalize(intersection_point);
		
	}
}


/// <summary>
/// Won't be implemented for a while, only if I have time.
/// </summary>
public class ParallelCamera :Camera
{
    public int SamplesPerPixel { get; set; }
    public float PlaneDistance { get; set; }
    
    public ParallelCamera(CameraConfig config) :base(config)
	{
		
    }
    public override Vector3d CastRay(Plane plane, int px_x, int px_y)
    {
        throw new NotImplementedException();
    }
}


/// <summary>
/// Represents an SceneObject in the scene. Going to be a tree structure, that stores the vertices, edges, planes etc...
/// </summary>
public class SceneObject:SceneEntity
{
	public override Vector3d Position { get; set; }
    
    public override void Translate(Vector3d translation)
    {
        throw new NotImplementedException();
    }

    public override void Rotate(Vector3d rotation)
    {
        throw new NotImplementedException();
    }
}
